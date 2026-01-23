using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace UI.Views.Generic;

[ContentProperty(nameof(EditorContent))]
public partial class AddEditWindowLayout : UserControl
{
    public static readonly DependencyProperty EditorContentProperty =
        DependencyProperty.Register(
            nameof(EditorContent),
            typeof(object),
            typeof(AddEditWindowLayout),
            new PropertyMetadata(null));

    public object? EditorContent
    {
        get => GetValue(EditorContentProperty);
        set => SetValue(EditorContentProperty, value);
    }

    public AddEditWindowLayout()
    {
        InitializeComponent();
    }
}
