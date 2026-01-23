using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using UI.Services;
using UI.ViewModels.Table;

namespace UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isCompetitionSelected;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    public MainViewModel(
        INavigationService navigationService,
        CompetitionSelectionViewModel competitionSelectionViewModel,
        EventsViewModel eventsViewModel,
        HeatsViewModel heatsViewModel,
        EntriesViewModel entriesViewModel,
        AthletesViewModel athletesViewModel,
        ClubsViewModel clubsViewModel,
        AgeGroupsViewModel ageGroupsViewModel,
        SwimStylesViewModel swimStylesViewModel)
    {
        _navigationService = navigationService;
        _navigationService.CurrentViewModelChanged += HandleNavigationViewModelChanged;

        CompetitionSelectionViewModel = competitionSelectionViewModel;
        CompetitionSelectionViewModel.CompetitionSelected += OnCompetitionSelected;
        CurrentViewModel = CompetitionSelectionViewModel;

        EventsViewModel = eventsViewModel;
        HeatsViewModel = heatsViewModel;
        EntriesViewModel = entriesViewModel;
        AthletesViewModel = athletesViewModel;
        ClubsViewModel = clubsViewModel;
        AgeGroupsViewModel = ageGroupsViewModel;
        SwimStylesViewModel = swimStylesViewModel;

        NavigateToEventsCommand = new NavigationCommand<EventsViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToHeatsCommand = new NavigationCommand<HeatsViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToEntriesCommand = new NavigationCommand<EntriesViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToAthletesCommand = new NavigationCommand<AthletesViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToClubsCommand = new NavigationCommand<ClubsViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToAgeGroupsCommand = new NavigationCommand<AgeGroupsViewModel>(_navigationService, () => _isCompetitionSelected);
        NavigateToSwimStylesCommand = new NavigationCommand<SwimStylesViewModel>(_navigationService, () => _isCompetitionSelected);
    }

    public CompetitionSelectionViewModel CompetitionSelectionViewModel { get; }

    public EventsViewModel EventsViewModel { get; }
    public HeatsViewModel HeatsViewModel { get; }
    public EntriesViewModel EntriesViewModel { get; }
    public AthletesViewModel AthletesViewModel { get; }
    public ClubsViewModel ClubsViewModel { get; }
    public AgeGroupsViewModel AgeGroupsViewModel { get; }
    public SwimStylesViewModel SwimStylesViewModel { get; }

    partial void OnIsCompetitionSelectedChanged(bool value)
    {
        ((NavigationCommand<EventsViewModel>)NavigateToEventsCommand).RaiseCanExecuteChanged();
        ((NavigationCommand<HeatsViewModel>)NavigateToHeatsCommand).RaiseCanExecuteChanged();
        ((NavigationCommand<EntriesViewModel>)NavigateToEntriesCommand).RaiseCanExecuteChanged();
        ((NavigationCommand<AthletesViewModel>)NavigateToAthletesCommand).RaiseCanExecuteChanged();
        ((NavigationCommand<ClubsViewModel>)NavigateToClubsCommand).RaiseCanExecuteChanged();
        ((NavigationCommand<AgeGroupsViewModel>)NavigateToAgeGroupsCommand).RaiseCanExecuteChanged();
        ((NavigationCommand<SwimStylesViewModel>)NavigateToSwimStylesCommand).RaiseCanExecuteChanged();
    }

    private void HandleNavigationViewModelChanged(ViewModelBase? viewModel)
    {
        CurrentViewModel = viewModel;
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