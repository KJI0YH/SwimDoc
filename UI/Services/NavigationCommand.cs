using System.Windows.Input;
using UI.ViewModels;

namespace UI.Services;

public class NavigationCommand<TViewModel> : ICommand
    where TViewModel : ViewModelBase
{
    private readonly Func<bool>? _canExecute;
    private readonly INavigationService _navigationService;

    public NavigationCommand(INavigationService navigationService, Func<bool>? canExecute = null)
    {
        _navigationService = navigationService;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter)) _navigationService.NavigateTo<TViewModel>(parameter);
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}