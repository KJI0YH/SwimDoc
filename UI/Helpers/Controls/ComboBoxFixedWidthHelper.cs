using System.Windows;
using System.Windows.Controls;

namespace UI.Helpers.Controls;

public static class ComboBoxFixedWidthHelper
{
    public static readonly DependencyProperty WidthProperty = DependencyProperty.RegisterAttached(
        "Width",
        typeof(double),
        typeof(ComboBoxFixedWidthHelper),
        new PropertyMetadata(double.NaN, OnWidthChanged));

    public static void SetWidth(DependencyObject element, double value) => element.SetValue(WidthProperty, value);

    public static double GetWidth(DependencyObject element) => (double)element.GetValue(WidthProperty);

    private static void OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element || e.NewValue is not double width || double.IsNaN(width))
            return;
        element.Width = width;
        element.MinWidth = width;
        element.MaxWidth = width;
    }
}
