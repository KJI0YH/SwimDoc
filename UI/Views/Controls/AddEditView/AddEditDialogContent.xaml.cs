using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace UI.Views.Controls.AddEditView;

[ContentProperty(nameof(EditorContent))]
public partial class AddEditDialogContent : UserControl
{
    public static readonly DependencyProperty EditorContentProperty =
        DependencyProperty.Register(
            nameof(EditorContent),
            typeof(object),
            typeof(AddEditDialogContent),
            new PropertyMetadata(null));

    public AddEditDialogContent()
    {
        InitializeComponent();
    }

    public object? EditorContent
    {
        get => GetValue(EditorContentProperty);
        set => SetValue(EditorContentProperty, value);
    }
}
