namespace UI.Services.Navigation;

public interface INavigationContextAware
{
    void ApplyContext(NavigationContext context);
}
