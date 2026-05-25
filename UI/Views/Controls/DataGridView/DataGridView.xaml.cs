using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DataLayer.EfClasses;
using UI.Helpers;
using UI.ViewModels;
using UI.ViewModels.Pages.Data;
using Wpf.Ui.Controls;
using DataGrid = System.Windows.Controls.DataGrid;

namespace UI.Views.Controls.DataGridView;

public partial class DataGridView : UserControl
{
    /// <summary>Light blue fill for selected entry rows (readable with default text color).</summary>
    private static readonly SolidColorBrush EntrySelectedRowBackground = CreateEntrySelectedRowBackground();
    private static readonly SolidColorBrush EntryHoverRowBackground = CreateEntryHoverRowBackground();

    private static SolidColorBrush CreateEntrySelectedRowBackground()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD));
        brush.Freeze();
        return brush;
    }

    private static SolidColorBrush CreateEntryHoverRowBackground()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0xE8, 0xEE, 0xF7));
        brush.Freeze();
        return brush;
    }

    public DataGridView()
    {
        InitializeComponent();
        DataContextChanged += GenericDataGridView_DataContextChanged;
    }

    private void GenericDataGridView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateColumns();
    }

    private void DataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateColumns();
        DgTableView.Focus();
        Keyboard.Focus(DgTableView);
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is DataViewModelBase vm)
            vm.SyncSelectedItemsFromGrid(DgTableView.SelectedItems);
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid || dataGrid.SelectedItems.Count == 0)
            return;

        if (DataContext is not ViewModelBase viewModel)
            return;

        var openDetailsCommand = viewModel.GetType().GetProperty("OpenDetailsCommand")?.GetValue(viewModel);
        if (openDetailsCommand is ICommand command && command.CanExecute(null))
        {
            command.Execute(null);
            e.Handled = true;
        }
    }

    private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (DataContext is not DataViewModelBase viewModel)
            return;

        var propertyType = GetPropertyType(e.PropertyName, viewModel);
        if (propertyType != null && propertyType.IsEnum &&
            e.Column is DataGridBoundColumn boundColumn &&
            boundColumn.Binding is Binding binding)
            binding.Converter = new EnumDescriptionConverter();
    }

    private void UpdateColumns()
    {
        if (DataContext is not DataViewModelBase viewModel)
        {
            DgTableView.RowStyle = null;
            return;
        }

        var columnConfigurations = viewModel.GetColumnConfigurations();
        var autoGenerate = viewModel.GetAutoGenerateColumns();

        if (autoGenerate || columnConfigurations.Count == 0)
        {
            DgTableView.AutoGenerateColumns = true;
            viewModel.ConfigureDataGrid(DgTableView);
            ApplyDataGridRowStyle(DgTableView, viewModel);
            return;
        }

        DgTableView.AutoGenerateColumns = false;
        DgTableView.Columns.Clear();

        var enumConverter = new EnumDescriptionConverter();

        foreach (var config in columnConfigurations)
        {
            var sortId = config.SortMemberPath ?? config.PropertyPath;
            var binding = new Binding(config.PropertyPath);

            if (config.Converter == null)
            {
                var propertyType = GetPropertyType(config.PropertyPath, viewModel);
                if (propertyType != null && propertyType.IsEnum)
                    binding.Converter = enumConverter;
            }
            else
            {
                binding.Converter = config.Converter as IValueConverter;
            }

            if (!string.IsNullOrEmpty(config.ConverterParameter))
                binding.ConverterParameter = config.ConverterParameter;

            DataGridColumn column;

            if (!string.IsNullOrWhiteSpace(config.TrueSymbolIcon) || !string.IsNullOrWhiteSpace(config.FalseSymbolIcon))
            {
                var root = new FrameworkElementFactory(typeof(Grid));

                if (!string.IsNullOrWhiteSpace(config.TrueSymbolIcon))
                {
                    var trueIcon = new FrameworkElementFactory(typeof(SymbolIcon));
                    trueIcon.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    trueIcon.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                    var trueSymbolValue = Enum.Parse(typeof(SymbolRegular), config.TrueSymbolIcon!, true);
                    trueIcon.SetValue(SymbolIcon.SymbolProperty, trueSymbolValue);

                    var trueVisibilityBinding = new Binding(config.PropertyPath)
                    {
                        Converter = new BoolToVisibilityConverter()
                    };
                    trueIcon.SetBinding(VisibilityProperty, trueVisibilityBinding);
                    root.AppendChild(trueIcon);
                }

                if (!string.IsNullOrWhiteSpace(config.FalseSymbolIcon))
                {
                    var falseIcon = new FrameworkElementFactory(typeof(SymbolIcon));
                    falseIcon.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    falseIcon.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                    falseIcon.SetValue(ForegroundProperty, Brushes.Gray);
                    var falseSymbolValue = Enum.Parse(typeof(SymbolRegular), config.FalseSymbolIcon!, true);
                    falseIcon.SetValue(SymbolIcon.SymbolProperty, falseSymbolValue);

                    var falseVisibilityBinding = new Binding(config.PropertyPath)
                    {
                        Converter = new BoolToVisibilityConverter(),
                        ConverterParameter = "Invert"
                    };
                    falseIcon.SetBinding(VisibilityProperty, falseVisibilityBinding);
                    root.AppendChild(falseIcon);
                }

                var template = new DataTemplate { VisualTree = root };

                column = new DataGridTemplateColumn
                {
                    CellTemplate = template,
                    Header = config.Header ?? config.PropertyPath,
                    IsReadOnly = config.IsReadOnly,
                    SortMemberPath = sortId
                };
            }
            else
            {
                column = new DataGridTextColumn
                {
                    Binding = binding,
                    Header = config.Header ?? config.PropertyPath,
                    IsReadOnly = config.IsReadOnly,
                    SortMemberPath = sortId
                };
            }

            if (config.Width.HasValue)
                column.Width = config.WidthUnitType.HasValue
                    ? new DataGridLength(config.Width.Value, config.WidthUnitType.Value)
                    : new DataGridLength(config.Width.Value);

            DgTableView.Columns.Add(column);
        }

        SyncDataGridSortGlyphs(viewModel);
        viewModel.ConfigureDataGrid(DgTableView);
        ApplyDataGridRowStyle(DgTableView, viewModel);
    }

    private static Type? ItemsEntityType(DataViewModelBase viewModel)
    {
        var itemsProp = viewModel.GetType().GetProperty("Items");
        if (itemsProp?.PropertyType is not { IsGenericType: true } t)
            return null;
        return t.GetGenericArguments()[0];
    }

    private static void ApplyDataGridRowStyle(DataGrid dataGrid, DataViewModelBase viewModel)
    {
        if (ItemsEntityType(viewModel) == typeof(Entry) &&
            dataGrid.TryFindResource("DataGridRowLightBlueWhenSelectedStyle") is Style entryRowStyle)
        {
            dataGrid.RowStyle = entryRowStyle;
            return;
        }

        dataGrid.RowStyle = CreateGenericSelectionRowStyle(dataGrid);
    }

    private static Style CreateGenericSelectionRowStyle(FrameworkElement lookup)
    {
        var rowStyle = new Style(typeof(DataGridRow));
        if (lookup.TryFindResource(typeof(DataGridRow)) is Style baseRowStyle)
            rowStyle.BasedOn = baseRowStyle;
        else if (Application.Current?.TryFindResource(typeof(DataGridRow)) is Style appRowStyle)
            rowStyle.BasedOn = appRowStyle;

        var hover = new MultiTrigger();
        hover.Conditions.Add(new Condition(DataGridRow.IsMouseOverProperty, true));
        hover.Conditions.Add(new Condition(DataGridRow.IsSelectedProperty, false));
        hover.Setters.Add(new Setter(Control.BackgroundProperty, EntryHoverRowBackground));
        rowStyle.Triggers.Add(hover);

        var selected = new Trigger
        {
            Property = DataGridRow.IsSelectedProperty,
            Value = true
        };
        selected.Setters.Add(new Setter(Control.BackgroundProperty, EntrySelectedRowBackground));
        rowStyle.Triggers.Add(selected);
        return rowStyle;
    }

    private static Type? GetPropertyType(string propertyPath, DataViewModelBase viewModel)
    {
        try
        {
            var itemsProperty = viewModel.GetType().GetProperty("Items");
            if (itemsProperty == null)
                return null;

            var itemsType = itemsProperty.PropertyType;
            if (!itemsType.IsGenericType)
                return null;

            var itemType = itemsType.GetGenericArguments()[0];
            var parts = propertyPath.Split('.');
            var currentType = itemType;

            foreach (var part in parts)
            {
                var property = currentType.GetProperty(part,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null)
                    return null;

                currentType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            }

            return currentType;
        }
        catch
        {
            return null;
        }
    }

    private void ColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is DataGridColumnHeader header && header.Column != null)
        {
            var columnName = header.Column.SortMemberPath;
            if (string.IsNullOrEmpty(columnName))
            {
                if (header.Column is DataGridBoundColumn boundColumn &&
                    boundColumn.Binding is Binding binding)
                    columnName = binding.Path?.Path ?? header.Column.Header?.ToString() ?? string.Empty;
                else
                    columnName = header.Column.Header?.ToString() ?? string.Empty;
            }

            if (DataContext is ViewModelBase viewModel)
            {
                var sortCommand = viewModel.GetType().GetProperty("SortByColumnCommand")?.GetValue(viewModel);
                if (sortCommand is ICommand command && command.CanExecute(columnName))
                {
                    command.Execute(columnName);
                    SyncDataGridSortGlyphs(viewModel);
                }
            }
        }

        e.Handled = true;
    }

    private void SyncDataGridSortGlyphs(ViewModelBase viewModel)
    {
        var type = viewModel.GetType();
        var sortColumn = type.GetProperty("SortColumn")?.GetValue(viewModel) as string;
        var dirObj = type.GetProperty("SortDirection")?.GetValue(viewModel);

        foreach (var col in DgTableView.Columns)
            col.SortDirection = null;

        if (string.IsNullOrEmpty(sortColumn) || dirObj is not ListSortDirection dir)
            return;

        foreach (var col in DgTableView.Columns)
            if (string.Equals(col.SortMemberPath, sortColumn, StringComparison.Ordinal))
            {
                col.SortDirection = dir;
                return;
            }
    }
}