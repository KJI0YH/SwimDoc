using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace UI.Helpers.Controls;

internal sealed class HorizontalScrollHook
{
    private const int WmMouseHwheel = 0x020E;
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
