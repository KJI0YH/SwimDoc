using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLayer.Crud;
using UI.ViewModels;

namespace UI.Views;

public partial class GenericEditWindow : Window
{
    private readonly object _entity;
    private readonly Type _entityType;
    private readonly bool _isNew;
    private readonly object _crudService;
    private readonly object _viewModel;
    private readonly List<FieldConfiguration> _fieldConfigurations;
    private readonly List<CustomValidationRule> _validationRules;

    public GenericEditWindow(object entity, bool isNew, object crudService, 
        List<FieldConfiguration>? fieldConfigurations = null, 
        List<CustomValidationRule>? validationRules = null)
    {
        _entity = entity;
        _entityType = entity.GetType();
        _isNew = isNew;
        _crudService = crudService;
        _fieldConfigurations = fieldConfigurations ?? new List<FieldConfiguration>();
        _validationRules = validationRules ?? new List<CustomValidationRule>();
        
        var viewModelType = typeof(GenericEditViewModel<,>).MakeGenericType(_entityType, typeof(int));
        var constructor = viewModelType.GetConstructor(new[] { _entityType, typeof(bool), typeof(ICrudService<,>).MakeGenericType(_entityType, typeof(int)), typeof(List<FieldConfiguration>), typeof(List<CustomValidationRule>) });
        
        if (constructor != null)
        {
            _viewModel = constructor.Invoke(new object[] { entity, isNew, crudService, _fieldConfigurations, _validationRules })!;
        }
        else
        {
            // Fallback для обратной совместимости
            _viewModel = Activator.CreateInstance(viewModelType, entity, isNew, crudService)!;
        }
        
        var closeRequestedEvent = viewModelType.GetEvent("CloseRequested");
        var handler = new EventHandler(OnCloseRequested);
        closeRequestedEvent!.AddEventHandler(_viewModel, handler);
        
        InitializeComponent();
        DataContext = _viewModel;
        
        CreatePropertyEditors();
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CreatePropertyEditors()
    {
        var propertyEditorsProperty = _viewModel.GetType().GetProperty("PropertyEditors");
        var propertyEditors = propertyEditorsProperty!.GetValue(_viewModel) as ObservableCollection<PropertyEditorViewModel>;
        
        var properties = _entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.Name != "Id")
            .ToList();

        // Если есть конфигурация полей, используем её
        if (_fieldConfigurations.Count > 0)
        {
            var configDict = _fieldConfigurations.ToDictionary(f => f.PropertyName, f => f);
            
            foreach (var config in _fieldConfigurations)
            {
                if (!config.IsVisible)
                    continue;

                var property = properties.FirstOrDefault(p => p.Name == config.PropertyName);
                if (property == null)
                    continue;

                var editor = CreateEditor(property, config);
                if (editor != null)
                {
                    var label = config.Label ?? GetPropertyLabel(property);
                    propertyEditors!.Add(new PropertyEditorViewModel
                    {
                        Label = label,
                        Editor = editor
                    });
                }
            }
        }
        else
        {
            // Автоматическая генерация всех полей
            foreach (var property in properties)
            {
                var editor = CreateEditor(property, null);
                if (editor != null)
                {
                    var label = GetPropertyLabel(property);
                    propertyEditors!.Add(new PropertyEditorViewModel
                    {
                        Label = label,
                        Editor = editor
                    });
                }
            }
        }
    }

    private FrameworkElement? CreateEditor(PropertyInfo property, FieldConfiguration? config)
    {
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var binding = new Binding(property.Name)
        {
            Source = _entity,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };

        FrameworkElement? editor = null;

        if (propertyType == typeof(string))
        {
            var textBox = new TextBox { Margin = new Thickness(0, 2, 0, 2) };
            if (config?.IsReadOnly == true)
                textBox.IsReadOnly = true;
            textBox.SetBinding(TextBox.TextProperty, binding);
            editor = textBox;
        }
        else if (propertyType == typeof(int))
        {
            var textBox = new TextBox { Margin = new Thickness(0, 2, 0, 2) };
            if (config?.IsReadOnly == true)
                textBox.IsReadOnly = true;
            textBox.SetBinding(TextBox.TextProperty, binding);
            editor = textBox;
        }
        else if (propertyType == typeof(bool))
        {
            var checkBox = new CheckBox { Margin = new Thickness(0, 2, 0, 2) };
            if (config?.IsReadOnly == true)
                checkBox.IsEnabled = false;
            checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
            editor = checkBox;
        }
        else if (propertyType.IsEnum)
        {
            var comboBox = new ComboBox
            {
                Margin = new Thickness(0, 2, 0, 2),
                ItemsSource = Enum.GetValues(propertyType)
            };
            if (config?.IsReadOnly == true)
                comboBox.IsEnabled = false;
            comboBox.SetBinding(ComboBox.SelectedItemProperty, binding);
            editor = comboBox;
        }
        else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
        {
            if (property.PropertyType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)))
            {
                return null;
            }
            return null;
        }
        else
        {
            var defaultTextBox = new TextBox { Margin = new Thickness(0, 2, 0, 2) };
            if (config?.IsReadOnly == true)
                defaultTextBox.IsReadOnly = true;
            defaultTextBox.SetBinding(TextBox.TextProperty, binding);
            editor = defaultTextBox;
        }

        return editor;
    }

    private string GetPropertyLabel(PropertyInfo property)
    {
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? property.Name;
    }
}

public static class GenericEditWindowFactory
{
    public static GenericEditWindow Create<TEntity, TKey>(TEntity entity, bool isNew, ICrudService<TEntity, TKey> crudService, 
        List<FieldConfiguration>? fieldConfigurations = null, 
        List<CustomValidationRule>? validationRules = null)
        where TEntity : class
    {
        return new GenericEditWindow(entity, isNew, crudService, fieldConfigurations, validationRules);
    }
}

public partial class GenericEditViewModel<TEntity, TKey> : ViewModelBase
    where TEntity : class
{
    private readonly TEntity _entity;
    private readonly bool _isNew;
    private readonly ICrudService<TEntity, TKey> _crudService;
    private readonly List<CustomValidationRule> _validationRules;

    [ObservableProperty]
    private ObservableCollection<string> _validationErrors = new();

    public GenericEditViewModel(TEntity entity, bool isNew, ICrudService<TEntity, TKey> crudService, 
        List<FieldConfiguration>? fieldConfigurations = null, 
        List<CustomValidationRule>? validationRules = null)
    {
        _entity = entity;
        _isNew = isNew;
        _crudService = crudService;
        _validationRules = validationRules ?? new List<CustomValidationRule>();
        PropertyEditors = new ObservableCollection<PropertyEditorViewModel>();
    }

    public string WindowTitle => _isNew ? "Создать новый элемент" : "Редактировать элемент";
    public ObservableCollection<PropertyEditorViewModel> PropertyEditors { get; }

    public bool HasErrors => ValidationErrors.Count > 0;

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationErrors.Clear();
        
        // Выполняем кастомную валидацию перед сохранением
        foreach (var rule in _validationRules)
        {
            try
            {
                var result = rule.Validator(_entity);
                if (result != System.ComponentModel.DataAnnotations.ValidationResult.Success)
                {
                    ValidationErrors.Add(rule.ErrorMessage);
                    return;
                }
            }
            catch (Exception ex)
            {
                ValidationErrors.Add($"Ошибка валидации: {ex.Message}");
                return;
            }
        }
        
        System.Collections.Immutable.ImmutableList<System.ComponentModel.DataAnnotations.ValidationResult> errors;
        
        if (_isNew)
        {
            var (entity, validationErrors) = await _crudService.CreateAsync(_entity);
            errors = validationErrors;
        }
        else
        {
            var (entity, validationErrors) = await _crudService.UpdateAsync(_entity);
            errors = validationErrors;
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                ValidationErrors.Add(error.ErrorMessage ?? "Ошибка валидации");
            }
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? CloseRequested;
}

public class PropertyEditorViewModel
{
    public string Label { get; set; } = string.Empty;
    public FrameworkElement Editor { get; set; } = null!;
}

