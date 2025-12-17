using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;

namespace UI.ViewModels;

public class FieldConfiguration
{
    public string PropertyName { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsReadOnly { get; set; } = false;
    public bool IsRequired { get; set; } = false;
    public object? DefaultValue { get; set; }
    public Type? EditorType { get; set; }
    public object? EditorParameter { get; set; }

    public static FieldConfiguration Create(string propertyName, string? label = null, bool isRequired = false)
    {
        return new FieldConfiguration
        {
            PropertyName = propertyName,
            Label = label,
            IsRequired = isRequired
        };
    }

    public static FieldConfiguration Hidden(string propertyName)
    {
        return new FieldConfiguration
        {
            PropertyName = propertyName,
            IsVisible = false
        };
    }
}

public class CustomValidationRule
{
    public Func<object, System.ComponentModel.DataAnnotations.ValidationResult> Validator { get; set; } = null!;
    public string ErrorMessage { get; set; } = string.Empty;
}

