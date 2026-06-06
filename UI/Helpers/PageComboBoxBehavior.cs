using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Helpers;

public static class PageComboBoxBehavior
{
    public static readonly DependencyProperty TotalPagesProperty =
        DependencyProperty.RegisterAttached(
            "TotalPages",
            typeof(int),
            typeof(PageComboBoxBehavior),
            new PropertyMetadata(0));

    public static void SetTotalPages(DependencyObject element, int value) => element.SetValue(TotalPagesProperty, value);
    public static int GetTotalPages(DependencyObject element) => (int)element.GetValue(TotalPagesProperty);

    public static readonly DependencyProperty CurrentPageProperty =
        DependencyProperty.RegisterAttached(
            "CurrentPage",
            typeof(int),
            typeof(PageComboBoxBehavior),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static void SetCurrentPage(DependencyObject element, int value) => element.SetValue(CurrentPageProperty, value);
    public static int GetCurrentPage(DependencyObject element) => (int)element.GetValue(CurrentPageProperty);

    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(PageComboBoxBehavior),
            new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox comboBox)
            return;

        if ((bool)e.NewValue)
        {
            comboBox.PreviewKeyDown += ComboBoxOnPreviewKeyDown;
            comboBox.LostKeyboardFocus += ComboBoxOnLostKeyboardFocus;
        }
        else
        {
            comboBox.PreviewKeyDown -= ComboBoxOnPreviewKeyDown;
            comboBox.LostKeyboardFocus -= ComboBoxOnLostKeyboardFocus;
        }
    }

    private static void ComboBoxOnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        CommitTextToCurrentPage((ComboBox)sender);
        e.Handled = true;
    }

    private static void ComboBoxOnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        CommitTextToCurrentPage((ComboBox)sender);
    }

    private static void CommitTextToCurrentPage(ComboBox comboBox)
    {
        if (!comboBox.IsEditable)
            return;

        var totalPages = GetTotalPages(comboBox);
        if (totalPages <= 0)
        {
            SetCurrentPage(comboBox, 0);
            comboBox.Text = string.Empty;
            return;
        }

        var text = comboBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            comboBox.Text = (GetCurrentPage(comboBox) + 1).ToString(CultureInfo.CurrentCulture);
            return;
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var oneBased))
        {
            comboBox.Text = (GetCurrentPage(comboBox) + 1).ToString(CultureInfo.CurrentCulture);
            return;
        }

        var clampedOneBased = Math.Max(1, Math.Min(totalPages, oneBased));
        var zeroBased = clampedOneBased - 1;

        SetCurrentPage(comboBox, zeroBased);
        comboBox.Text = clampedOneBased.ToString(CultureInfo.CurrentCulture);
    }
}
