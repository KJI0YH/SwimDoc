using System.Windows.Controls;

namespace UI.ViewModels.Generic;

public class ColumnConfiguration
{
    public string PropertyPath { get; set; } = string.Empty;
    public string? Header { get; set; }
    public double? Width { get; set; }
    public DataGridLengthUnitType? WidthUnitType { get; set; }
    public bool IsReadOnly { get; set; } = true;
    public string? DisplayMemberPath { get; set; }
    public object? Converter { get; set; }
    public string? ConverterParameter { get; set; }
    public string? TrueSymbolIcon { get; set; }
    public string? FalseSymbolIcon { get; set; }
    public static ColumnConfiguration Create(string propertyPath, string? header = null, double? width = null)
    {
        return new ColumnConfiguration
        {
            PropertyPath = propertyPath,
            Header = header,
            Width = width
        };
    }
}

