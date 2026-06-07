using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace UI.Helpers.Controls;

public static class FixationTimeInputNavigation
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(FixationTimeInputNavigation),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Control control)
            return;
        if (e.NewValue is true)
            control.PreviewKeyDown += OnPreviewKeyDown;
        else
            control.PreviewKeyDown -= OnPreviewKeyDown;
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Up or Key.Down))
            return;
        if (sender is not Control currentControl)
            return;
        var dataGrid = FindParent<DataGrid>(currentControl);
        if (dataGrid?.ItemsSource is not IEnumerable items)
            return;
        var list = items.Cast<object>().ToList();
        var currentItem = currentControl.DataContext;
        var index = list.IndexOf(currentItem!);
        if (index < 0)
            return;
        var nextIndex = e.Key == Key.Down ? index + 1 : index - 1;
        if (nextIndex < 0 || nextIndex >= list.Count)
            return;
        var nextItem = list[nextIndex];
        e.Handled = true;
        dataGrid.SelectedItem = nextItem;
        dataGrid.ScrollIntoView(nextItem);
        currentControl.Dispatcher.BeginInvoke(
            () => FocusTimeInput(dataGrid, nextItem),
            DispatcherPriority.Input);
    }

    private static void FocusTimeInput(DataGrid dataGrid, object item)
    {
        dataGrid.UpdateLayout();
        if (dataGrid.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row)
            return;
        if (FindTimeInput(row) is not Control nextControl)
            return;
        nextControl.Focus();
        if (nextControl is TextBox textBox)
            textBox.SelectAll();
    }

    private static Control? FindTimeInput(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Control control && GetIsEnabled(control))
                return control;
            var found = FindTimeInput(child);
            if (found is not null)
                return found;
        }
        return null;
    }

    private static T? FindParent<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
