using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Views.Components;

public partial class ImportInfoBar : UserControl
{
    public ImportInfoBar()
    {
        InitializeComponent();
    }

    private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row) return;
        if (FilesGrid == null) return;

        // Toggle: clicking the already-selected row collapses RowDetails
        if (row.IsSelected)
        {
            FilesGrid.SelectedItem = null;
            e.Handled = true;
            return;
        }

        // Prevent selecting rows without warnings/errors (so RowDetails won't open)
        var (warnings, errors) = GetWarningsAndErrorsCount(row.Item);
        if (warnings == 0 && errors == 0)
        {
            FilesGrid.SelectedItem = null;
            e.Handled = true;
        }
    }

    private static (int warnings, int errors) GetWarningsAndErrorsCount(object? item)
    {
        if (item is null) return (0, 0);

        var type = item.GetType();
        var warningsProp = type.GetProperty("WarningsCount");
        var errorsProp = type.GetProperty("ErrorsCount");

        var warnings = warningsProp?.GetValue(item) is int w ? w : 0;
        var errors = errorsProp?.GetValue(item) is int er ? er : 0;
        return (warnings, errors);
    }
}

