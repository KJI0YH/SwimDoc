using System.ComponentModel;
using System.Windows.Controls;

namespace UI.Models.Grid;

public class ColumnConfiguration
{
    public string PropertyPath { get; set; } = string.Empty;
    public string? SortMemberPath { get; set; }
    public string? Header { get; set; }
    public double? Width { get; set; }
    public DataGridLengthUnitType? WidthUnitType { get; set; }
    public bool IsReadOnly { get; set; } = true;
    public object? Converter { get; set; }
    public string? ConverterParameter { get; set; }
    public string? TrueSymbolIcon { get; set; }
    public string? FalseSymbolIcon { get; set; }
    internal string GetSortKey()
    {
        return SortMemberPath ?? PropertyPath;
    }
}
