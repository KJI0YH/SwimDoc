using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI.ViewModels.Pages;

namespace UI.Views.Controls.EventFilterComboBox;

public partial class EventFilterComboBox : UserControl
{
    public event EventHandler? DropDownOpened;
    private Window? _hostWindow;
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(EventFilterComboBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DisplayTextProperty =
        DependencyProperty.Register(
            nameof(DisplayText),
            typeof(string),
            typeof(EventFilterComboBox),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(EventFilterComboBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty FilterWidthProperty =
        DependencyProperty.Register(
            nameof(FilterWidth),
            typeof(double),
            typeof(EventFilterComboBox),
            new PropertyMetadata(double.NaN, OnFilterWidthChanged));

    public EventFilterComboBox()
    {
        InitializeComponent();
        Unloaded += (_, _) => DetachOutsideClickHandler();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public double FilterWidth
    {
        get => (double)GetValue(FilterWidthProperty);
        set => SetValue(FilterWidthProperty, value);
    }

    private static void OnFilterWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not EventFilterComboBox control || e.NewValue is not double width || double.IsNaN(width))
            return;
        control.Width = width;
        control.MinWidth = width;
        control.MaxWidth = width;
    }

    private void OnDropDownOpened()
    {
        DropDownOpened?.Invoke(this, EventArgs.Empty);
        _hostWindow = Window.GetWindow(this);
        if (_hostWindow == null)
            return;
        _hostWindow.PreviewMouseDown += HostWindow_OnPreviewMouseDown;
    }

    private void DropDownToggle_OnChecked(object sender, RoutedEventArgs e) => OnDropDownOpened();
    private void DropDownToggle_OnUnchecked(object sender, RoutedEventArgs e) => DetachOutsideClickHandler();
    private void PopupRoot_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindFilterOption(e.OriginalSource as DependencyObject) is not { } option)
            return;
        option.IsSelected = !option.IsSelected;
        this.RefreshDisplayText();
        e.Handled = true;
    }

    private void HostWindow_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DropDownToggle.IsChecked != true)
            return;
        if (IsMouseOver)
            return;
        if (IsWithinPopup(e.OriginalSource as DependencyObject))
            return;
        DropDownToggle.IsChecked = false;
    }

    private bool IsWithinPopup(DependencyObject? source)
    {
        while (source != null)
        {
            if (ReferenceEquals(source, PopupRoot))
                return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    private static IEventFilterOption? FindFilterOption(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is FrameworkElement { DataContext: IEventFilterOption option })
                return option;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }

    private void DetachOutsideClickHandler()
    {
        if (_hostWindow == null)
            return;
        _hostWindow.PreviewMouseDown -= HostWindow_OnPreviewMouseDown;
        _hostWindow = null;
    }
}
