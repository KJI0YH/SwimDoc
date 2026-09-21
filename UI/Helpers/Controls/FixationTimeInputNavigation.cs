using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace UI.Helpers.Controls;

/// <summary>
/// Arrow-key focus between time inputs inside a DataGrid.
/// Attach <see cref="IsEnabledProperty"/> to each editable time TextBox.
/// ↑/↓ — same column, adjacent row; ←/→ — adjacent time cell in the row.
/// </summary>
public static class FixationTimeInputNavigation
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(FixationTimeInputNavigation),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static readonly KeyEventHandler PreviewKeyDownHandler = OnPreviewKeyDown;

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;
        if (e.NewValue is true)
            element.AddHandler(Keyboard.PreviewKeyDownEvent, PreviewKeyDownHandler, handledEventsToo: true);
        else
            element.RemoveHandler(Keyboard.PreviewKeyDownEvent, PreviewKeyDownHandler);
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is not (Key.Up or Key.Down or Key.Left or Key.Right))
            return;

        var currentControl = FindEnabledTimeInput(sender as DependencyObject)
                             ?? FindEnabledTimeInput(e.OriginalSource as DependencyObject);
        if (currentControl is null)
            return;
        if (!ShouldNavigateFromTextCaret(currentControl, key))
            return;

        var dataGrid = FindParent<DataGrid>(currentControl);
        if (dataGrid?.ItemsSource is not IEnumerable items)
            return;

        var list = items.Cast<object>().ToList();
        var currentItem = ResolveItem(currentControl, list);
        if (currentItem is null)
            return;
        var rowIndex = list.IndexOf(currentItem);
        if (rowIndex < 0)
            return;

        var currentRow = FindParent<DataGridRow>(currentControl);
        if (currentRow is null)
            return;
        var rowEditors = GetRowTimeInputs(currentRow).ToList();
        var editorIndex = IndexOfEditor(rowEditors, currentControl);
        if (editorIndex < 0)
            return;

        var targetRowIndex = rowIndex;
        var targetEditorIndex = editorIndex;
        switch (key)
        {
            case Key.Up:
                targetRowIndex = rowIndex - 1;
                break;
            case Key.Down:
                targetRowIndex = rowIndex + 1;
                break;
            case Key.Left:
                targetEditorIndex = editorIndex - 1;
                break;
            case Key.Right:
                targetEditorIndex = editorIndex + 1;
                break;
        }

        if (targetRowIndex < 0 || targetRowIndex >= list.Count)
            return;
        if (key is Key.Left or Key.Right)
        {
            if (targetEditorIndex < 0 || targetEditorIndex >= rowEditors.Count)
                return;
        }

        e.Handled = true;
        var targetItem = list[targetRowIndex];
        var focusEditorIndex = key is Key.Left or Key.Right ? targetEditorIndex : editorIndex;
        dataGrid.SelectedItem = targetItem;
        dataGrid.CurrentItem = targetItem;
        dataGrid.ScrollIntoView(targetItem);
        currentControl.Dispatcher.BeginInvoke(
            () => FocusTimeInput(dataGrid, targetItem, focusEditorIndex, attempt: 0),
            DispatcherPriority.Input);
    }

    private static bool ShouldNavigateFromTextCaret(Control control, Key key)
    {
        var textBox = FindInnerTextBox(control) ?? control as TextBox;
        if (textBox is null)
            return true;
        if (textBox.SelectionLength == textBox.Text.Length && textBox.Text.Length > 0)
            return true;
        return key switch
        {
            Key.Left => textBox.CaretIndex <= 0 && textBox.SelectionLength == 0,
            Key.Right => textBox.CaretIndex >= textBox.Text.Length && textBox.SelectionLength == 0,
            Key.Up or Key.Down => true,
            _ => false
        };
    }

    private static void FocusTimeInput(DataGrid dataGrid, object item, int editorIndex, int attempt)
    {
        dataGrid.UpdateLayout();
        if (dataGrid.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row)
        {
            if (attempt >= 8)
                return;
            dataGrid.ScrollIntoView(item);
            dataGrid.Dispatcher.BeginInvoke(
                () => FocusTimeInput(dataGrid, item, editorIndex, attempt + 1),
                DispatcherPriority.Loaded);
            return;
        }

        var editors = GetRowTimeInputs(row).ToList();
        if (editorIndex < 0 || editorIndex >= editors.Count)
            return;

        var nextControl = editors[editorIndex];
        var focusTarget = FindInnerTextBox(nextControl) ?? (UIElement)nextControl;
        focusTarget.Focusable = true;
        Keyboard.Focus(focusTarget);
        focusTarget.Focus();
        if (focusTarget is TextBox textBox)
            textBox.SelectAll();
    }

    private static Control? FindEnabledTimeInput(DependencyObject? current)
    {
        while (current is not null)
        {
            if (current is Control control && GetIsEnabled(control))
                return control;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static object? ResolveItem(DependencyObject from, List<object> items)
    {
        var current = from;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: { } dc } && items.Contains(dc))
                return dc;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static int IndexOfEditor(List<Control> editors, Control current)
    {
        for (var i = 0; i < editors.Count; i++)
        {
            if (ReferenceEquals(editors[i], current))
                return i;
            if (IsDescendantOf(current, editors[i]) || IsDescendantOf(editors[i], current))
                return i;
        }
        return -1;
    }

    private static bool IsDescendantOf(DependencyObject? node, DependencyObject ancestor)
    {
        while (node is not null)
        {
            if (ReferenceEquals(node, ancestor))
                return true;
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }

    private static IEnumerable<Control> GetRowTimeInputs(DataGridRow row)
    {
        var result = new List<Control>();
        CollectTimeInputs(row, result);
        return result
            .OrderBy(c =>
            {
                try
                {
                    return c.TransformToAncestor(row).Transform(new Point(0, 0)).X;
                }
                catch
                {
                    return 0;
                }
            });
    }

    private static void CollectTimeInputs(DependencyObject root, List<Control> result)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Control control && GetIsEnabled(control) && control.IsVisible && control.IsEnabled)
                result.Add(control);
            CollectTimeInputs(child, result);
        }
    }

    private static TextBox? FindInnerTextBox(DependencyObject root)
    {
        if (root is TextBox textBox)
            return textBox;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var found = FindInnerTextBox(VisualTreeHelper.GetChild(root, i));
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
