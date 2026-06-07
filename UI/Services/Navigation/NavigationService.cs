using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using UI.Helpers.Navigation;
using UI.ViewModels;
using UI.ViewModels.Pages;

namespace UI.Services.Navigation;

public class NavigationService(IServiceProvider serviceProvider) : INavigationService
{
    private readonly Stack<NavigationFrame> _backStack = new();
    private readonly Dictionary<Type, object?> _navigationParameters = new();
    private readonly Dictionary<Type, Type> _viewModelToViewMapping = new();
    private NavigationFrame? _currentFrame;

    public ViewModelBase? CurrentViewModel => _currentFrame?.ViewModel;
    public bool CanGoBack => _backStack.Count > 0;
    public IReadOnlyList<NavigationFrame> BackStackFrames => _backStack.Reverse().ToArray();
    public event Action<ViewModelBase?>? CurrentViewModelChanged;
    public event Action<NavigationState>? NavigationStateChanged;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase =>
        NavigateTo<TViewModel>(null);

    public void NavigateTo<TViewModel>(object? parameter) where TViewModel : ViewModelBase
    {
        var context = NavigationContext.Parse(parameter);
        var viewModel = serviceProvider.GetRequiredService<TViewModel>();
        _navigationParameters[typeof(TViewModel)] = context;
        var frame = CreateFrame(viewModel, typeof(TViewModel), ResolveSidebarTag(typeof(TViewModel)), context);
        MoveTo(frame, addCurrentToBackStack: true, isRestore: false);
    }

    public void NavigateTo<TViewModel>(NavigationContext context) where TViewModel : ViewModelBase =>
        NavigateTo<TViewModel>((object?)context);

    public void NavigateToRoot<TViewModel>() where TViewModel : ViewModelBase
    {
        _backStack.Clear();
        _navigationParameters.Clear();
        if (_currentFrame?.ViewModel is INavigationAware leaving)
            leaving.OnNavigatedFrom();
        var viewModel = serviceProvider.GetRequiredService<TViewModel>();
        _navigationParameters.Remove(typeof(TViewModel));
        _currentFrame = CreateFrame(
            viewModel,
            typeof(TViewModel),
            NavigationPageRegistry.ViewModelTypeToSidebarTag.GetValueOrDefault(typeof(TViewModel)),
            null);
        EnterFrame(_currentFrame, isRestore: false);
        PublishState(NavigationTransition.Root);
    }

    public void NavigateToCompetitionSelection()
    {
        _backStack.Clear();
        _navigationParameters.Clear();
        if (_currentFrame?.ViewModel is INavigationAware leaving)
            leaving.OnNavigatedFrom();
        var viewModel = serviceProvider.GetRequiredService<CompetitionSelectionViewModel>();
        _currentFrame = CreateFrame(
            viewModel,
            typeof(CompetitionSelectionViewModel),
            null,
            null);
        EnterFrame(_currentFrame, isRestore: false);
        PublishState(NavigationTransition.Root);
    }

    public void NavigateSidebar(string tag)
    {
        if (!NavigationPageRegistry.SidebarTagToViewModelType.TryGetValue(tag, out var viewModelType))
            return;
        if (_currentFrame is not null
            && _currentFrame.SidebarTag == tag
            && NavigationPageRegistry.RootViewModelTypes.Contains(_currentFrame.ViewModelType))
            return;
        var viewModel = (ViewModelBase)serviceProvider.GetRequiredService(viewModelType);
        _navigationParameters.Remove(viewModelType);
        var frame = CreateFrame(viewModel, viewModelType, tag, null);
        MoveTo(frame, addCurrentToBackStack: _currentFrame is not null, isRestore: false);
    }

    public NavigationContext? GetNavigationContext<TViewModel>() where TViewModel : ViewModelBase =>
        GetNavigationParameter<TViewModel>() as NavigationContext;

    public object? GetNavigationParameter<TViewModel>() where TViewModel : ViewModelBase =>
        _navigationParameters.TryGetValue(typeof(TViewModel), out var parameter) ? parameter : null;

    public void GoBack()
    {
        if (!CanGoBack)
            return;
        if (_currentFrame?.ViewModel is INavigationAware leaving)
            leaving.OnNavigatedFrom();
        _currentFrame = _backStack.Pop();
        RestoreNavigationParameter(_currentFrame);
        EnterFrame(_currentFrame, isRestore: true);
        PublishState(NavigationTransition.Back);
    }

    public void ResetForNewCompetition()
    {
        _backStack.Clear();
        _navigationParameters.Clear();
        _currentFrame = null;
    }

    public void RegisterMapping<TViewModel, TView>()
        where TViewModel : ViewModelBase
        where TView : UserControl
    {
        _viewModelToViewMapping[typeof(TViewModel)] = typeof(TView);
    }

    public Type? GetViewType<TViewModel>() where TViewModel : ViewModelBase =>
        _viewModelToViewMapping.TryGetValue(typeof(TViewModel), out var viewType) ? viewType : null;

    private void MoveTo(NavigationFrame frame, bool addCurrentToBackStack, bool isRestore)
    {
        if (addCurrentToBackStack && _currentFrame is not null)
            _backStack.Push(CaptureFrame(_currentFrame));
        if (_currentFrame?.ViewModel is INavigationAware leaving)
            leaving.OnNavigatedFrom();
        _currentFrame = frame;
        EnterFrame(_currentFrame, isRestore);
        PublishState(NavigationTransition.Forward);
    }

    private void EnterFrame(NavigationFrame frame, bool isRestore)
    {
        ApplyTabIndex(frame.ViewModel, frame.TabIndex);
        if (frame.ViewModel is INavigationAware navigationAware)
        {
            if (isRestore)
                navigationAware.OnNavigationRestored();
            else
                navigationAware.OnNavigatedTo(frame.Parameter);
        }
        if (frame.ViewModel is IDataLoadable loadable)
            loadable.EnsureDataLoaded();
    }

    private void PublishState(NavigationTransition transition)
    {
        if (_currentFrame is null)
            return;
        CurrentViewModelChanged?.Invoke(_currentFrame.ViewModel);
        NavigationStateChanged?.Invoke(new NavigationState(
            _currentFrame.PageType,
            _currentFrame.ViewModel,
            _currentFrame.SidebarTag,
            _currentFrame.TabIndex,
            CanGoBack,
            transition));
    }

    private NavigationFrame CreateFrame(
        ViewModelBase viewModel,
        Type viewModelType,
        string? sidebarTag,
        NavigationContext? parameter) =>
        new(
            viewModel,
            viewModelType,
            NavigationPageRegistry.ViewModelTypeToPageType[viewModelType],
            sidebarTag,
            ReadTabIndex(viewModel),
            parameter);

    private NavigationFrame CaptureFrame(NavigationFrame frame)
    {
        var parameter = _navigationParameters.TryGetValue(frame.ViewModelType, out var stored)
            ? NavigationContext.Parse(stored)
            : frame.Parameter;
        return frame with
        {
            TabIndex = ReadTabIndex(frame.ViewModel),
            Parameter = parameter
        };
    }

    private void RestoreNavigationParameter(NavigationFrame frame)
    {
        if (frame.Parameter is not null)
            _navigationParameters[frame.ViewModelType] = frame.Parameter;
        else
            _navigationParameters.Remove(frame.ViewModelType);
    }

    private string? ResolveSidebarTag(Type targetViewModelType)
    {
        if (NavigationPageRegistry.RootViewModelTypes.Contains(targetViewModelType))
            return NavigationPageRegistry.ViewModelTypeToSidebarTag.GetValueOrDefault(targetViewModelType);
        return _currentFrame?.SidebarTag;
    }

    private static int ReadTabIndex(ViewModelBase viewModel) =>
        viewModel is INavigationTabState tabState ? tabState.NavigationTabIndex : 0;

    private static void ApplyTabIndex(ViewModelBase viewModel, int tabIndex)
    {
        if (viewModel is INavigationTabState tabState)
            tabState.NavigationTabIndex = tabIndex;
    }
}
