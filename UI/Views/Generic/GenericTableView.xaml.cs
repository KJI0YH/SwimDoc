using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using UI.Helpers;
using UI.ViewModels;
using UI.ViewModels.Generic;
using Wpf.Ui.Controls;

namespace UI.Views.Generic;

public partial class GenericTableView : UserControl
{
    public static readonly DependencyProperty ToolbarProperty = DependencyProperty.Register(
        nameof(Toolbar),
        typeof(object),
        typeof(GenericTableView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ToolbarContentProperty = DependencyProperty.Register(
        nameof(ToolbarContent),
        typeof(object),
        typeof(GenericTableView),
        new PropertyMetadata(null));

    public object? Toolbar
    {
        get => GetValue(ToolbarProperty);
        set => SetValue(ToolbarProperty, value);
    }

    public object? ToolbarContent
    {
        get => GetValue(ToolbarContentProperty);
        set => SetValue(ToolbarContentProperty, value);
    }

    public GenericTableView()
    {
        InitializeComponent();
        DataContextChanged += GenericTableView_DataContextChanged;
    }

    private void GenericTableView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateColumns();
    }

    private void DataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateColumns();
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is GenericTableViewModelBase vm)
            vm.SyncSelectedItemsFromGrid(DgTableView.SelectedItems);
    }

    private void  DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (DataContext is not GenericTableViewModelBase viewModel) return;
        
        var propertyType = GetPropertyType(e.PropertyName, viewModel);
        if (propertyType != null && propertyType.IsEnum)
        {
            if (e.Column is DataGridBoundColumn boundColumn && boundColumn.Binding is Binding binding)
            {
                binding.Converter = new EnumDescriptionConverter();
            }
        }
    }

    private void UpdateColumns()
    {
        if (DataContext is not GenericTableViewModelBase viewModel)
        {
            return;
        }

        var columnConfigurations = viewModel.GetColumnConfigurations();
        var autoGenerate = viewModel.GetAutoGenerateColumns();

        if (autoGenerate || columnConfigurations.Count == 0)
        {
            DgTableView.AutoGenerateColumns = true;
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
                {
                    binding.Converter = enumConverter;
                }
            }
            else
            {
                binding.Converter = config.Converter as IValueConverter;
            }

            if (!string.IsNullOrEmpty(config.ConverterParameter))
            {
                binding.ConverterParameter = config.ConverterParameter;
            }

            DataGridColumn column;

            if (!string.IsNullOrWhiteSpace(config.TrueSymbolIcon) || !string.IsNullOrWhiteSpace(config.FalseSymbolIcon))
            {
                var root = new FrameworkElementFactory(typeof(Grid));

                // true icon
                if (!string.IsNullOrWhiteSpace(config.TrueSymbolIcon))
                {
                    var trueIcon = new FrameworkElementFactory(typeof(SymbolIcon));
                    trueIcon.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    trueIcon.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                    var trueSymbolValue = Enum.Parse(typeof(SymbolRegular), config.TrueSymbolIcon!, ignoreCase: true);
                    trueIcon.SetValue(SymbolIcon.SymbolProperty, trueSymbolValue);

                    var trueVisibilityBinding = new Binding(config.PropertyPath)
                    {
                        Converter = new BoolToVisibilityConverter()
                    };
                    trueIcon.SetBinding(VisibilityProperty, trueVisibilityBinding);
                    root.AppendChild(trueIcon);
                }

                // false icon
                if (!string.IsNullOrWhiteSpace(config.FalseSymbolIcon))
                {
                    var falseIcon = new FrameworkElementFactory(typeof(SymbolIcon));
                    falseIcon.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    falseIcon.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                    falseIcon.SetValue(ForegroundProperty, System.Windows.Media.Brushes.Gray);
                    var falseSymbolValue = Enum.Parse(typeof(SymbolRegular), config.FalseSymbolIcon!, ignoreCase: true);
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
                    SortMemberPath = sortId,
                };
            }
            else
            {
                column = new DataGridTextColumn
                {
                    Binding = binding,
                    Header = config.Header ?? config.PropertyPath,
                    IsReadOnly = config.IsReadOnly,
                    SortMemberPath = sortId,
                };
            }

            if (config.Width.HasValue)
            {
                column.Width = config.WidthUnitType.HasValue
                    ? new DataGridLength(config.Width.Value, config.WidthUnitType.Value)
                    : new DataGridLength(config.Width.Value);
            }

            DgTableView.Columns.Add(column);
        }

        SyncDataGridSortGlyphs(viewModel);
    }

    private Type? GetPropertyType(string propertyPath, GenericTableViewModelBase viewModel)
    {
        try
        {

            var itemsProperty = viewModel.GetType().GetProperty("Items");
            if (itemsProperty == null) return null;
            
            var itemsType = itemsProperty.PropertyType;
            if (!itemsType.IsGenericType) return null;
            
            var itemType = itemsType.GetGenericArguments()[0];
            
            var parts = propertyPath.Split('.');
            var currentType = itemType;
            
            foreach (var part in parts)
            {
                var property = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null) return null;
                
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
                {
                    columnName = binding.Path?.Path ?? header.Column.Header?.ToString() ?? string.Empty;
                }
                else
                {
                    columnName = header.Column.Header?.ToString() ?? string.Empty;
                }
            }

            if (DataContext is ViewModelBase viewModel)
            {
                var sortCommand = viewModel.GetType().GetProperty("SortByColumnCommand")?.GetValue(viewModel);
                if (sortCommand is System.Windows.Input.ICommand command && command.CanExecute(columnName))
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
        {
            if (string.Equals(col.SortMemberPath, sortColumn, StringComparison.Ordinal))
            {
                col.SortDirection = dir;
                return;
            }
        }
    }
}

