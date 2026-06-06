using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.Services;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class AthleteDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly EntriesByAthleteViewModel _entriesTable;
    private readonly HeatsByAthleteViewModel _heatsTable;
    private readonly ResultsByAthleteViewModel _resultsTable;
    [ObservableProperty] private string? _title = string.Empty;
    [ObservableProperty] private int _selectedTabIndex;

    public AthleteDetailsViewModel(
        IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService,
        IEventService eventService,
        IHeatService heatService,
        INavigationService navigationService)
    {
        _entriesTable = new EntriesByAthleteViewModel(entryService, entryDocumentReaderService);
        _heatsTable = new HeatsByAthleteViewModel(eventService, heatService, navigationService);
        _resultsTable = new ResultsByAthleteViewModel(entryService);
    }

    public ViewModelBase EntriesTable => _entriesTable;
    public ViewModelBase HeatsTable => _heatsTable;
    public ViewModelBase ResultsTable => _resultsTable;

    public void OnNavigatedTo(object? parameter)
    {
        int athleteId;
        int? focusEntryId = null;
        int? focusSwimEventId = null;

        switch (parameter)
        {
            case AthleteDetailsNavigationParameter navigation:
                athleteId = navigation.AthleteId;
                focusEntryId = navigation.FocusEntryId;
                focusSwimEventId = navigation.FocusSwimEventId;
                SelectedTabIndex = navigation.OpenHeatsTab ? 1 : 0;
                break;
            case int idValue:
                athleteId = idValue;
                SelectedTabIndex = 0;
                break;
            default:
                return;
        }

        _entriesTable.SetAthleteId(athleteId);
        _heatsTable.SetAthleteId(athleteId, focusEntryId, focusSwimEventId);
        _resultsTable.SetAthleteId(athleteId);
    }
}