using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace UI.Helpers;

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

    private sealed class HorizontalScrollHook
    {
        private readonly DataGrid _dataGrid;
        private readonly ScrollViewer _scrollViewer;
        private HwndSource? _hwndSource;
        private HwndSourceHook? _wndProc;

        public HorizontalScrollHook(DataGrid dataGrid, ScrollViewer scrollViewer)
        {
            _dataGrid = dataGrid;
            _scrollViewer = scrollViewer;
        }

        public void Attach()
        {
            _dataGrid.PreviewMouseWheel += OnPreviewMouseWheel;
            _dataGrid.Unloaded += OnDataGridUnloaded;

            _hwndSource = PresentationSource.FromVisual(_dataGrid) as HwndSource;
            if (_hwndSource is null)
                return;

            _wndProc = WndProc;
            _hwndSource.AddHook(_wndProc);
        }

        public void Detach()
        {
            _dataGrid.PreviewMouseWheel -= OnPreviewMouseWheel;
            _dataGrid.Unloaded -= OnDataGridUnloaded;

            if (_hwndSource is not null && _wndProc is not null)
                _hwndSource.RemoveHook(_wndProc);

            _hwndSource = null;
            _wndProc = null;
        }

        private void OnDataGridUnloaded(object sender, RoutedEventArgs e) => Detach();

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || _scrollViewer.ScrollableWidth <= 0)
                return;

            _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WmMouseHwheel || _scrollViewer.ScrollableWidth <= 0)
                return IntPtr.Zero;

            if (!IsMouseOverDataGrid())
                return IntPtr.Zero;

            var delta = (short)((uint)wParam.ToInt64() >> 16);
            _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + delta);
            handled = true;
            return IntPtr.Zero;
        }

        private bool IsMouseOverDataGrid()
        {
            if (!_dataGrid.IsVisible || PresentationSource.FromVisual(_dataGrid) is null)
                return false;

            var position = Mouse.GetPosition(_dataGrid);
            return position.X >= 0 && position.Y >= 0 &&
                   position.X <= _dataGrid.ActualWidth && position.Y <= _dataGrid.ActualHeight;
        }
    }
}
