using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using UI.Helpers;
using UI.ViewModels;
using UI.ViewModels.Generic;

namespace UI.Views.Generic;

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

    private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        // Применяем конвертер для enum колонок при автогенерации
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
            DataGrid.AutoGenerateColumns = true;
            return;
        }

        DataGrid.AutoGenerateColumns = false;
        DataGrid.Columns.Clear();

        var enumConverter = new EnumDescriptionConverter();
        
        foreach (var config in columnConfigurations)
        {
            var binding = new Binding(config.PropertyPath);
            
            // Если конвертер не указан явно, проверяем, является ли свойство enum
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

