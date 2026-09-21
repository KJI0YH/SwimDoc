using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using UI.ViewModels.Pages;
using BaseTimesView = UI.Views.Controls.BaseTimesSettingsView.BaseTimesSettingsView;
using RankTimesView = UI.Views.Controls.RankTimesSettingsView.RankTimesSettingsView;
using ScoringView = UI.Views.Controls.ScoringSettingsView.ScoringSettingsView;

namespace UI.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.PropertyChanged += OnViewModelPropertyChanged;
        SyncHubContentWidth();
        EnsureLazyHosts();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.PropertyChanged -= OnViewModelPropertyChanged;
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.IsScoringOpen)
            or nameof(SettingsViewModel.IsRankTimesOpen)
            or nameof(SettingsViewModel.IsBaseTimesOpen))
            EnsureLazyHosts();
    }

    private void EnsureLazyHosts()
    {
        if (DataContext is not SettingsViewModel vm)
            return;

        if (vm.IsScoringOpen && ScoringHost.Content is null)
            ScoringHost.Content = new ScoringView();

        if (vm.IsRankTimesOpen && RankTimesHost.Content is null)
        {
            RankTimesHost.Content = new RankTimesView
            {
                DataContext = vm.RankTimes
            };
        }

        if (vm.IsBaseTimesOpen && BaseTimesHost.Content is null)
        {
            BaseTimesHost.Content = new BaseTimesView
            {
                DataContext = vm.BaseTimes
            };
        }
    }
}
