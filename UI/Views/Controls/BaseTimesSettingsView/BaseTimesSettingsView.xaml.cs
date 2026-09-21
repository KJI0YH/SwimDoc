using System.Windows;
using System.Windows.Controls;

namespace UI.Views.Controls.BaseTimesSettingsView;

public partial class BaseTimesSettingsView : UserControl
{
    public static readonly DependencyProperty ShowHeaderProperty =
        DependencyProperty.Register(
            nameof(ShowHeader),
            typeof(bool),
            typeof(BaseTimesSettingsView),
            new PropertyMetadata(true));

    public bool ShowHeader
    {
        get => (bool)GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    public BaseTimesSettingsView()
    {
        InitializeComponent();
    }
}
