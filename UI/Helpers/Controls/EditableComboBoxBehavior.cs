using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace UI.Helpers.Controls;

public static class EditableComboBoxBehavior
{
  private static readonly DependencyProperty IsHookedProperty =
      DependencyProperty.RegisterAttached(
          "IsHooked",
          typeof(bool),
          typeof(EditableComboBoxBehavior),
          new PropertyMetadata(false));

  public static readonly DependencyProperty IsEnabledProperty =
      DependencyProperty.RegisterAttached(
          "IsEnabled",
          typeof(bool),
          typeof(EditableComboBoxBehavior),
          new PropertyMetadata(false, OnIsEnabledChanged));

  public static void SetIsEnabled(DependencyObject element, bool value) =>
      element.SetValue(IsEnabledProperty, value);

  public static bool GetIsEnabled(DependencyObject element) =>
      (bool)element.GetValue(IsEnabledProperty);

  private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not ComboBox comboBox)
      return;
    if ((bool)e.NewValue)
    {
      comboBox.Loaded += OnComboBoxLoaded;
      TryHook(comboBox);
    }
    else
    {
      comboBox.Loaded -= OnComboBoxLoaded;
      Unhook(comboBox);
    }
  }

  private static void OnComboBoxLoaded(object sender, RoutedEventArgs e)
  {
    if (sender is ComboBox comboBox)
      TryHook(comboBox);
  }

  private static void TryHook(ComboBox comboBox)
  {
    if (!GetIsEnabled(comboBox) || !comboBox.IsEditable)
      return;
    comboBox.ApplyTemplate();
    if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is not TextBox textBox)
      return;
    if ((bool)textBox.GetValue(IsHookedProperty))
      return;
    textBox.SetValue(IsHookedProperty, true);
    textBox.PreviewTextInput += OnEditableTextBoxPreviewTextInput;
    textBox.TextChanged += OnEditableTextBoxTextChanged;
  }

  private static void Unhook(ComboBox comboBox)
  {
    comboBox.ApplyTemplate();
    if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is not TextBox textBox)
      return;
    if (!(bool)textBox.GetValue(IsHookedProperty))
      return;
    textBox.PreviewTextInput -= OnEditableTextBoxPreviewTextInput;
    textBox.TextChanged -= OnEditableTextBoxTextChanged;
    textBox.ClearValue(IsHookedProperty);
  }

  private static void OnEditableTextBoxPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
  {
    if (sender is not TextBox textBox)
      return;
    textBox.Dispatcher.BeginInvoke(
      () => CollapseSelectionToEnd(textBox),
      DispatcherPriority.Input);
  }

  private static void OnEditableTextBoxTextChanged(object sender, TextChangedEventArgs e)
  {
    if (sender is TextBox textBox)
      CollapseSelectionToEnd(textBox);
  }

  public static void CollapseSelectionToEnd(TextBox? textBox)
  {
    if (textBox is null || textBox.SelectionLength == 0)
      return;
    textBox.CaretIndex = textBox.SelectionStart + textBox.SelectionLength;
    textBox.SelectionLength = 0;
  }
}
