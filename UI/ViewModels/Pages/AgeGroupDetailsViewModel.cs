using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class AgeGroupDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly AthletesByAgeGroupViewModel _athletesTable;
    private readonly EventsByAgeGroupViewModel _eventsTable;
    private readonly CombinedResultsByAgeGroupViewModel _combinedResultsTable;
    [ObservableProperty] private string? _title = string.Empty;

    public AgeGroupDetailsViewModel(
        IAgeGroupService ageGroupService,
        IAthleteService athleteService,
        IEventService eventService,
        IEntryService entryService)
    {
        _athletesTable = new AthletesByAgeGroupViewModel(athleteService, ageGroupService);
        _eventsTable = new EventsByAgeGroupViewModel(eventService);
        _combinedResultsTable = new CombinedResultsByAgeGroupViewModel();
    }

    public ViewModelBase AthletesTable => _athletesTable;
    public ViewModelBase EventsTable => _eventsTable;
    public ViewModelBase CombinedResultsTable => _combinedResultsTable;
    public void OnNavigatedTo(object? parameter)
    {
        if (NavigationContext.Parse(parameter)?.ResolveId() is not int idValue)
            return;
        _athletesTable.SetAgeGroupId(idValue);
        _eventsTable.SetAgeGroupId(idValue);
        _combinedResultsTable.SetAgeGroupId(idValue);
    }
}
