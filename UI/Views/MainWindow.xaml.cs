using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using UI.Services;
using UI.ViewModels;
using UI.Views.Pages;
using Wpf.Ui.Controls;

namespace UI.Views;

public partial class MainWindow : FluentWindow
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.Current.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;
    }

    private void NavigationView_OnLoaded(object sender, RoutedEventArgs e)
    {
        NavigationView.SetServiceProvider(App.Current.Services);
        NavigationView.Navigate(typeof(CompetitionSelectionPage));

        var navigationService = App.Current.Services.GetRequiredService<INavigationService>();
        navigationService.PageNavigationRequested += OnPageNavigationRequested;
    }

    private void OnPageNavigationRequested(Type pageType)
    {
        Dispatcher.Invoke(() => NavigationView.Navigate(pageType));
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    public void ShowModalOverlay()
    {
        ModalOverlay.Visibility = Visibility.Visible;
    }
    
    public void HideModalOverlay()
    {
        ModalOverlay.Visibility = Visibility.Collapsed;
    }
}