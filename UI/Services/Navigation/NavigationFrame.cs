using UI.ViewModels;

namespace UI.Services.Navigation;

public sealed record NavigationFrame(
    ViewModelBase ViewModel,
    Type ViewModelType,
    Type PageType,
    string? SidebarTag,
    int TabIndex,
    NavigationContext? Parameter);
