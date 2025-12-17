using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using UI.ViewModels;

namespace UI.Views;

public partial class GenericTableView : UserControl
{
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
            DataGrid.AutoGenerateColumns = true;
            return;
        }

        DataGrid.AutoGenerateColumns = false;
        DataGrid.Columns.Clear();

        foreach (var config in columnConfigurations)
        {
            var binding = new Binding(config.PropertyPath);
            
            if (config.Converter != null)
            {
                binding.Converter = config.Converter as IValueConverter;
            }

            if (!string.IsNullOrEmpty(config.ConverterParameter))
            {
                binding.ConverterParameter = config.ConverterParameter;
            }

            var column = new DataGridTextColumn
            {
                Binding = binding,
                Header = config.Header ?? config.PropertyPath,
                IsReadOnly = config.IsReadOnly,
                SortMemberPath = config.PropertyPath
            };

            if (config.Width.HasValue)
            {
                column.Width = config.WidthUnitType.HasValue
                    ? new DataGridLength(config.Width.Value, config.WidthUnitType.Value)
                    : new DataGridLength(config.Width.Value);
            }

            DataGrid.Columns.Add(column);
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
                }
            }
        }
    }
}

public abstract class GenericTableViewModelBase : ViewModelBase
{
    public abstract System.Collections.ObjectModel.ObservableCollection<ColumnConfiguration> GetColumnConfigurations();
    public abstract bool GetAutoGenerateColumns();
}

