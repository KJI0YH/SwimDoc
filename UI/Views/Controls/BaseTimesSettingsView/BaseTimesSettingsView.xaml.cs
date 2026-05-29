using System.Windows;
using System.Windows.Controls;

namespace UI.Views.Controls.BaseTimesSettingsView;

public partial class BaseTimesSettingsView : UserControl
{
    public BaseTimesSettingsView()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncContentWidth();
    }

    private void LayoutRoot_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        SyncContentWidth();
    }

    private void SyncContentWidth()
    {
        if (ContentGrid is null)
            return;

        var width = BaseTimesScroll.ViewportWidth > 0
            ? BaseTimesScroll.ViewportWidth
            : LayoutRoot.ActualWidth;

        if (width <= 0)
            return;

        ContentGrid.Width = width;
    }
}
