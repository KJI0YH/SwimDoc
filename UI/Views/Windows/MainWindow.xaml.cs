using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using UI.Helpers.Navigation;
using UI.Resources;
using UI.Services.Navigation;
using UI.ViewModels;
using UI.Views.Pages;
using Wpf.Ui.Controls;
using MainViewModel = UI.ViewModels.Windows.MainViewModel;

namespace UI.Views.Windows;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel? _viewModel;
    private INavigationService? _navigationService;
    private bool _isApplyingNavigationState;
    private bool _backButtonHooked;

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
        NavigationView.BackRequested += OnNavigationBackRequested;
        NavigationView.Navigating += OnNavigationNavigating;
        NavigationView.PreviewMouseDown += OnNavigationPreviewMouseDown;
        _navigationService = App.Current.Services.GetRequiredService<INavigationService>();
        _navigationService.NavigationStateChanged += OnNavigationStateChanged;
        if (_viewModel is not { IsCompetitionSelected: true })
            _navigationService.NavigateToCompetitionSelection();
        Dispatcher.BeginInvoke(HookBackButton, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void HookBackButton()
    {
        if (_backButtonHooked)
            return;
        NavigationView.ApplyTemplate();
        if (NavigationView.Template.FindName("PART_BackButton", NavigationView) is not System.Windows.Controls.Button backButton)
            return;
        backButton.PreviewMouseLeftButtonDown += OnBackButtonPreviewClick;
        _backButtonHooked = true;
    }

    private void OnNavigationStateChanged(NavigationState state)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => OnNavigationStateChanged(state));
            return;
        }

        _isApplyingNavigationState = true;
        try
        {
            var page = (Page)App.Current.Services.GetRequiredService(state.PageType);
            page.DataContext = state.ViewModel;
            NavigationView.ReplaceContent(page);

            if (state.SidebarTag is not null)
                UpdateSidebarActiveState(state.SidebarTag);

            UpdateBackButtonState(state.CanGoBack);
            HookBackButton();
        }
        finally
        {
            _isApplyingNavigationState = false;
        }
    }

    private void UpdateSidebarActiveState(string activeTag)
    {
        SetSidebarActive(NavigationView.MenuItems, activeTag);
        SetSidebarActive(NavigationView.FooterMenuItems, activeTag);
    }

    private static void SetSidebarActive(System.Collections.IEnumerable items, string activeTag)
    {
        foreach (var item in items)
        {
            if (item is not NavigationViewItem menuItem)
                continue;
            menuItem.IsActive = string.Equals(menuItem.Tag as string, activeTag, StringComparison.Ordinal);
        }
    }

    private void OnNavigationNavigating(NavigationView sender, NavigatingCancelEventArgs args)
    {
        if (_isApplyingNavigationState || _navigationService is null)
            return;
        if (_viewModel is not { IsCompetitionSelected: true })
            return;

        var pageType = args.Page.GetType();
        if (!NavigationPageRegistry.IsRootPage(pageType))
            return;
        if (!NavigationPageRegistry.PageTypeToSidebarTag.TryGetValue(pageType, out var tag))
            return;

        args.Cancel = true;
        _navigationService.NavigateSidebar(tag);
    }

    private void OnNavigationBackRequested(object sender, RoutedEventArgs e) =>
        TryGoBack();

    private void OnBackButtonPreviewClick(object sender, MouseButtonEventArgs e)
    {
        if (!TryGoBack())
            return;
        e.Handled = true;
    }

    private void OnNavigationPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.XButton1)
            return;
        if (!TryGoBack())
            return;
        e.Handled = true;
    }

    private bool TryGoBack()
    {
        if (_navigationService is null || !_navigationService.CanGoBack)
            return false;
        _navigationService.GoBack();
        return true;
    }

    private void UpdateBackButtonState(bool canGoBack)
    {
        var showBack = _viewModel is { IsCompetitionSelected: true } && canGoBack;
        NavigationView.IsBackButtonVisible = showBack
            ? NavigationViewBackButtonVisible.Visible
            : NavigationViewBackButtonVisible.Collapsed;
        NavigationView.SetCurrentValue(NavigationView.IsBackEnabledProperty, showBack);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

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

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

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
}
