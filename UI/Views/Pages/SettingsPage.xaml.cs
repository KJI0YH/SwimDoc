using System.Windows;
using System.Windows.Controls;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => SyncHubContentWidth();
    }

    private void SettingsHubRoot_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        SyncHubContentWidth();
    }

    private void SyncHubContentWidth()
    {
        if (SettingsHubContent is null)
            return;
        var width = SettingsHubScroll.ViewportWidth > 0
            ? SettingsHubScroll.ViewportWidth
            : SettingsHubRoot.ActualWidth;
        if (width <= 0)
            return;
        SettingsHubContent.Width = width;
    }
}
