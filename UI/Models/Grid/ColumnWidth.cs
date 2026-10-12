using System.Windows.Controls;

namespace UI.Models.Grid;

public static class ColumnWidth
{
    public static DataGridLength Auto => DataGridLength.Auto;
    public static DataGridLength Fill => new(1, DataGridLengthUnitType.Star);
}
