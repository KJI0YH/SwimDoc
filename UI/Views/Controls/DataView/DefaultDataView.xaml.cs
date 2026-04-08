using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace UI.Views.Controls.DataView;

[ContentProperty(nameof(MainContent))]
public partial class DefaultDataView : UserControl
{
    public static readonly DependencyProperty ToolbarProperty = DependencyProperty.Register(
        nameof(Toolbar),
        typeof(object),
        typeof(DefaultDataView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty MainContentProperty = DependencyProperty.Register(
        nameof(MainContent),
        typeof(object),
        typeof(DefaultDataView),
        new PropertyMetadata(null));

    public DefaultDataView()
    {
        InitializeComponent();
    }

    public object? Toolbar
    {
        get => GetValue(ToolbarProperty);
        set => SetValue(ToolbarProperty, value);
    }

    public object? MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }
}