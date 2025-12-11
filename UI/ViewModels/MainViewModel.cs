using System.Windows.Input;
using UI.Services;

namespace UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private bool _isCompetitionSelected;

    public MainViewModel(INavigationService navigationService, CompetitionSelectionViewModel competitionSelectionViewModel)
    {
        _navigationService = navigationService;
        _navigationService.CurrentViewModelChanged += OnCurrentViewModelChanged;

        CompetitionSelectionViewModel = competitionSelectionViewModel;
        CompetitionSelectionViewModel.CompetitionSelected += OnCompetitionSelected;
        CurrentViewModel = CompetitionSelectionViewModel;
        
        NavigateToEventsCommand = new NavigationCommand<EventsViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToHeatsCommand = new NavigationCommand<HeatsViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToEntriesCommand = new NavigationCommand<EntriesViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToAthletesCommand = new NavigationCommand<AthletesViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToClubsCommand = new NavigationCommand<ClubsViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToAgeGroupsCommand = new NavigationCommand<AgeGroupsViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToSwimStylesCommand = new NavigationCommand<SwimStylesViewModel>(_navigationService, () => _isCompetitionSelected);
    }

    public CompetitionSelectionViewModel CompetitionSelectionViewModel { get; }

    private ViewModelBase? _currentViewModel;
    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetField(ref _currentViewModel, value);
    }

    private void OnCurrentViewModelChanged(ViewModelBase? viewModel)
    {
        CurrentViewModel = viewModel;
    }

    public bool IsCompetitionSelected
    {
        get => _isCompetitionSelected;
        private set
        {
            if (SetField(ref _isCompetitionSelected, value))
            {
                ((NavigationCommand<EventsViewModel>)NavigateToEventsCommand).RaiseCanExecuteChanged();
                ((NavigationCommand<HeatsViewModel>)NavigateToHeatsCommand).RaiseCanExecuteChanged();
                ((NavigationCommand<EntriesViewModel>)NavigateToEntriesCommand).RaiseCanExecuteChanged();
                ((NavigationCommand<AthletesViewModel>)NavigateToAthletesCommand).RaiseCanExecuteChanged();
                ((NavigationCommand<ClubsViewModel>)NavigateToClubsCommand).RaiseCanExecuteChanged();
                ((NavigationCommand<AgeGroupsViewModel>)NavigateToAgeGroupsCommand).RaiseCanExecuteChanged();
                ((NavigationCommand<SwimStylesViewModel>)NavigateToSwimStylesCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private void OnCompetitionSelected(string filePath)
    {
        IsCompetitionSelected = true;
        _navigationService.NavigateTo<EventsViewModel>();
    }

    public ICommand NavigateToEventsCommand { get; }
    public ICommand NavigateToHeatsCommand { get; }
    public ICommand NavigateToEntriesCommand { get; }
    public ICommand NavigateToAthletesCommand { get; }
    public ICommand NavigateToClubsCommand { get; }
    public ICommand NavigateToAgeGroupsCommand { get; }
    public ICommand NavigateToSwimStylesCommand { get; }
}