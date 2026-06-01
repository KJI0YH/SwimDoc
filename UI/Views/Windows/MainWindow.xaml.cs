using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using UI.Helpers;
using UI.Resources;
using UI.Services;
using UI.Views.Pages;
using Wpf.Ui.Controls;
using MainViewModel = UI.ViewModels.Windows.MainViewModel;

namespace UI.Views.Windows;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.Current.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;

        StateChanged += (_, _) => UpdateRestoreButtonIcon();
        Loaded += OnMainWindowLoaded;
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        UpdateRestoreButtonIcon();

        var contentDialogService = App.Current.Services.GetRequiredService<Wpf.Ui.IContentDialogService>();
        contentDialogService.SetDialogHost(RootContentDialog);
    }

    private void NavigationView_OnLoaded(object sender, RoutedEventArgs e)
    {
        NavigationView.SetServiceProvider(App.Current.Services);

        // NavigationViewContentPresenter is assigned in OnApplyTemplate, which can run after Loaded.
        Dispatcher.BeginInvoke(() => NavigationView.Navigate(typeof(CompetitionSelectionPage)), System.Windows.Threading.DispatcherPriority.Loaded);

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

    private void TitleBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (WindowDragHelper.IsInteractiveChrome(e.OriginalSource as DependencyObject))
            return;

        WindowDragHelper.HandleDrag(this, e, ToggleMaximizeRestore);
    }

    private void ToggleMaximizeRestore() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
        UpdateRestoreButtonIcon();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateRestoreButtonIcon()
    {
        if (RestoreButton == null)
            return;

        var isMaximized = WindowState == WindowState.Maximized;
        RestoreButton.Content = isMaximized
            ? new SymbolIcon { Symbol = SymbolRegular.SquareMultiple24 }
            : new SymbolIcon { Symbol = SymbolRegular.Square24 };
        RestoreButton.ToolTip = isMaximized ? Strings.Window_Restore : Strings.Window_Maximize;
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