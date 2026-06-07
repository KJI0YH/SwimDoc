using System.Windows;
using System.Windows.Data;
using UI.Views.Controls.EventFilterComboBox;

namespace UI.Helpers.Controls;

public static class EventFilterComboBoxBehavior
{
    public static readonly DependencyProperty IsMultiSelectFilterProperty =
        DependencyProperty.RegisterAttached(
            "IsMultiSelectFilter",
            typeof(bool),
            typeof(EventFilterComboBoxBehavior),
            new PropertyMetadata(false, OnIsMultiSelectFilterChanged));

    public static bool GetIsMultiSelectFilter(DependencyObject element) =>
        (bool)element.GetValue(IsMultiSelectFilterProperty);

    public static void SetIsMultiSelectFilter(DependencyObject element, bool value) =>
        element.SetValue(IsMultiSelectFilterProperty, value);

    private static void OnIsMultiSelectFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not EventFilterComboBox control)
            return;
        if ((bool)e.NewValue)
            control.DropDownOpened += OnDropDownOpened;
        else
            control.DropDownOpened -= OnDropDownOpened;
    }

    private static void OnDropDownOpened(object? sender, EventArgs e)
    {
        if (sender is not EventFilterComboBox control)
            return;
        control.RefreshDisplayText();
    }

    public static void RefreshDisplayText(this EventFilterComboBox control)
    {
        BindingOperations.GetBindingExpression(control, EventFilterComboBox.DisplayTextProperty)
            ?.UpdateTarget();
    }
}
