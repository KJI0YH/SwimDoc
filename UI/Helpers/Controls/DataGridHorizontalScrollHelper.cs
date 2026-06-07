using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace UI.Helpers.Controls;

public static class DataGridHorizontalScrollHelper
{
    private const int WmMouseHwheel = 0x020E;
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridHorizontalScrollHelper),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty HookProperty =
        DependencyProperty.RegisterAttached(
            "Hook",
            typeof(HorizontalScrollHook),
            typeof(DataGridHorizontalScrollHelper),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
            return;
        if ((bool)e.NewValue)
        {
            dataGrid.Loaded += OnDataGridLoaded;
            if (dataGrid.IsLoaded)
                AttachHook(dataGrid);
        }
        else
        {
            dataGrid.Loaded -= OnDataGridLoaded;
            DetachHook(dataGrid);
        }
    }

    private static void OnDataGridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
            AttachHook(dataGrid);
    }

    private static void AttachHook(DataGrid dataGrid)
    {
        if (GetHook(dataGrid) is not null)
            return;
        var scrollViewer = FindScrollViewer(dataGrid);
        if (scrollViewer is null)
            return;
        var hook = new HorizontalScrollHook(dataGrid, scrollViewer);
        SetHook(dataGrid, hook);
        hook.Attach();
    }

    private static void DetachHook(DataGrid dataGrid)
    {
        if (GetHook(dataGrid) is not HorizontalScrollHook hook)
            return;
        hook.Detach();
        SetHook(dataGrid, null);
    }

    private static HorizontalScrollHook? GetHook(DependencyObject dataGrid) =>
        dataGrid.GetValue(HookProperty) as HorizontalScrollHook;

    private static void SetHook(DependencyObject dataGrid, HorizontalScrollHook? hook) =>
        dataGrid.SetValue(HookProperty, hook);

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer scrollViewer)
            return scrollViewer;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var found = FindScrollViewer(child);
            if (found is not null)
                return found;
        }
        return null;
    }
}
