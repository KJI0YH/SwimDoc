using UI.ViewModels;

namespace UI.Services.Navigation;

public enum NavigationTransition
{
    Forward,
    Back,
    Root
}

public sealed record NavigationState(
    Type PageType,
    ViewModelBase ViewModel,
    string? SidebarTag,
    int TabIndex,
    bool CanGoBack,
    NavigationTransition Transition);
