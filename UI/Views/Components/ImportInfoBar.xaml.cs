using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace UI.Views.Components;

public partial class ImportInfoBar : UserControl
{
    private static readonly BooleanToVisibilityConverter BoolToVis = new();

    public ImportInfoBar()
    {
        InitializeComponent();
    }

    private void FilesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        // Keep default row visuals (hover/selection) by avoiding RowStyle;
        // bind details visibility per item instead.
        BindingOperations.SetBinding(
            e.Row,
            DataGridRow.DetailsVisibilityProperty,
            new Binding("IsDetailsOpen")
            {
                Converter = BoolToVis
            });
    }

    private void FilesGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FilesGrid == null) return;

        if (IsClickInsideRowDetails(e.OriginalSource as DependencyObject))
            return;

        var row = ItemsControl.ContainerFromElement(FilesGrid, e.OriginalSource as DependencyObject) as DataGridRow;
        if (row == null) return;

        var (warnings, errors) = GetWarningsAndErrorsCount(row.Item);
        if (warnings == 0 && errors == 0)
        {
            e.Handled = true;
            return;
        }

        ToggleIsDetailsOpen(row.Item);
        e.Handled = true;
    }

    private static void ToggleIsDetailsOpen(object? item)
    {
        if (item is null) return;

        var type = item.GetType();
        var isSummaryRowProp = type.GetProperty("IsSummaryRow");
        if (isSummaryRowProp?.GetValue(item) is bool isSummary && isSummary)
            return;

        var prop = type.GetProperty("IsDetailsOpen");
        if (prop?.PropertyType != typeof(bool) || !prop.CanRead || !prop.CanWrite) return;

        var current = prop.GetValue(item) is bool b && b;
        prop.SetValue(item, !current);
    }
    
    private static bool IsClickInsideRowDetails(DependencyObject? source)
    {
        var current = source;
        while (current != null)
        {
            if (current is DataGridDetailsPresenter)
                return true;
            current = GetParentSafe(current);
        }
        return false;
    }

    private static DependencyObject? GetParentSafe(DependencyObject current)
    {
        // VisualTreeHelper throws for non-Visuals (e.g. Run/TextElement).
        if (current is Visual or Visual3D)
            return VisualTreeHelper.GetParent(current);

        return LogicalTreeHelper.GetParent(current);
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

