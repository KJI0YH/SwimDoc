using System.Windows;
using System.Windows.Controls;

namespace UI.Views.Controls.DataGridView;

public partial class EntriesView : UserControl
{
    public static readonly DependencyProperty ShowImportActionsProperty =
        DependencyProperty.Register(
            nameof(ShowImportActions),
            typeof(bool),
            typeof(EntriesView),
            new PropertyMetadata(true));

    public bool ShowImportActions
    {
        get => (bool)GetValue(ShowImportActionsProperty);
        set => SetValue(ShowImportActionsProperty, value);
    }

    public EntriesView()
    {
        InitializeComponent();
    }
}
