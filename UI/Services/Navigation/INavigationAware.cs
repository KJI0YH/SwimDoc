namespace UI.Services.Navigation;

public interface INavigationAware
{
    void OnNavigatedTo(object? parameter);
    void OnNavigatedFrom()
    {
    }
}
