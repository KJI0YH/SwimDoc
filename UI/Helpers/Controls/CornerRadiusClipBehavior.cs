using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Helpers.Controls;

public static class CornerRadiusClipBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(CornerRadiusClipBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        element.SizeChanged -= OnSizeChanged;
        element.Loaded -= OnLoaded;
        if ((bool)e.NewValue)
        {
            element.SizeChanged += OnSizeChanged;
            element.Loaded += OnLoaded;
            UpdateClip(element);
        }
        else
        {
            element.Clip = null;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
            UpdateClip(element);
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement element)
            UpdateClip(element);
    }

    private static void UpdateClip(FrameworkElement element)
    {
        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            element.Clip = null;
            return;
        }

        var radius = element is Border border
            ? MaxCornerRadius(border.CornerRadius)
            : 0;
        element.Clip = new RectangleGeometry(
            new Rect(0, 0, element.ActualWidth, element.ActualHeight),
            radius,
            radius);
    }

    private static double MaxCornerRadius(CornerRadius cornerRadius) =>
        Math.Max(
            Math.Max(cornerRadius.TopLeft, cornerRadius.TopRight),
            Math.Max(cornerRadius.BottomLeft, cornerRadius.BottomRight));
}
