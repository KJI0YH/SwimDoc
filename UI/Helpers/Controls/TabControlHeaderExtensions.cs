using System.Windows;
using System.Windows.Controls;

namespace UI.Helpers.Controls;

public static class TabControlHeaderExtensions
{
    public static readonly DependencyProperty RightHeaderContentProperty =
        DependencyProperty.RegisterAttached(
            "RightHeaderContent",
            typeof(object),
            typeof(TabControlHeaderExtensions),
            new PropertyMetadata(null));

    public static object? GetRightHeaderContent(DependencyObject element) =>
        element.GetValue(RightHeaderContentProperty);

    public static void SetRightHeaderContent(DependencyObject element, object? value) =>
        element.SetValue(RightHeaderContentProperty, value);
}
