using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
 
namespace UI.Helpers;
 
public static class ComboBoxAutoWidthHelper
{
    public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
        "Enable",
        typeof(bool),
        typeof(ComboBoxAutoWidthHelper),
        new PropertyMetadata(false, OnEnableChanged));
 
    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);
 
    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox comboBox)
            return;
 
        if (e.NewValue is true)
        {
            comboBox.Loaded += ComboBoxOnLoaded;
            comboBox.DropDownOpened += ComboBoxOnDropDownOpened;
            comboBox.ItemContainerGenerator.StatusChanged += (_, _) => UpdateWidth(comboBox);
        }
        else
        {
            comboBox.Loaded -= ComboBoxOnLoaded;
            comboBox.DropDownOpened -= ComboBoxOnDropDownOpened;
        }
    }
 
    private static void ComboBoxOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
            return;
 
        UpdateWidth(comboBox);
    }
 
    private static void ComboBoxOnDropDownOpened(object? sender, EventArgs e)
    {
        if (sender is ComboBox comboBox)
            UpdateWidth(comboBox);
    }
 
    private static void UpdateWidth(ComboBox comboBox)
    {
        var items = comboBox.ItemsSource ?? comboBox.Items;
        if (items is null)
            return;
 
        var displayMemberPath = comboBox.DisplayMemberPath;
 
        var typeface = new Typeface(
            comboBox.FontFamily,
            comboBox.FontStyle,
            comboBox.FontWeight,
            comboBox.FontStretch);
 
        var dpi = VisualTreeHelper.GetDpi(comboBox).PixelsPerDip;
        double maxTextWidth = 0;
 
        foreach (var item in (IEnumerable)items)
        {
            if (item is null)
                continue;
 
            var text = GetDisplayText(item, displayMemberPath);
            if (string.IsNullOrEmpty(text))
                continue;
 
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                comboBox.FontSize,
                Brushes.Black,
                dpi);
 
            maxTextWidth = Math.Max(maxTextWidth, ft.WidthIncludingTrailingWhitespace);
        }
 
        if (maxTextWidth <= 0)
            return;
 
        // Add room for padding + drop-down button + a bit of breathing space.
        var padding = comboBox.Padding;
        var extra = padding.Left + padding.Right + 46;
 
        comboBox.Width = Math.Ceiling(maxTextWidth + extra);
    }
 
    private static string GetDisplayText(object item, string displayMemberPath)
    {
        if (string.IsNullOrWhiteSpace(displayMemberPath))
            return item.ToString() ?? string.Empty;
 
        var prop = item.GetType().GetProperty(displayMemberPath);
        var value = prop?.GetValue(item);
        return value?.ToString() ?? string.Empty;
    }
}

