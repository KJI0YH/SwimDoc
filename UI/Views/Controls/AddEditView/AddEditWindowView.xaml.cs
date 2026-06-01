using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using UI.Helpers;

namespace UI.Views.Controls.AddEditView;

[ContentProperty(nameof(EditorContent))]
public partial class AddEditWindowView : UserControl
{
    public static readonly DependencyProperty EditorContentProperty =
        DependencyProperty.Register(
            nameof(EditorContent),
            typeof(object),
            typeof(AddEditWindowView),
            new PropertyMetadata(null));

    public AddEditWindowView()
    {
        InitializeComponent();
    }

    public object? EditorContent
    {
        get => GetValue(EditorContentProperty);
        set => SetValue(EditorContentProperty, value);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        WindowDragHelper.HandleDrag(Window.GetWindow(this), e);
}