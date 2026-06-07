using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Helpers.Controls;

public static class WindowDragHelper
{
    public static bool IsInteractiveChrome(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is Button or MenuItem or TextBox or ComboBox or Slider)
                return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    public static void HandleDrag(Window? window, MouseButtonEventArgs e, Action? onDoubleClick = null)
    {
        if (window is null || e.ChangedButton != MouseButton.Left)
            return;
        if (e.ClickCount == 2)
        {
            onDoubleClick?.Invoke();
            e.Handled = true;
            return;
        }
        if (e.LeftButton != MouseButtonState.Pressed)
            return;
        if (window.WindowState == WindowState.Maximized && window.ResizeMode != ResizeMode.NoResize)
            RestoreFromMaximizedBeforeDrag(window, e);
        try
        {
            window.DragMove();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void RestoreFromMaximizedBeforeDrag(Window window, MouseButtonEventArgs e)
    {
        var cursorScreen = window.PointToScreen(e.GetPosition(window));
        var widthRatio = e.GetPosition(window).X / window.ActualWidth;
        window.WindowState = WindowState.Normal;
        window.Left = cursorScreen.X - widthRatio * window.ActualWidth;
        window.Top = cursorScreen.Y - 12;
    }
}
