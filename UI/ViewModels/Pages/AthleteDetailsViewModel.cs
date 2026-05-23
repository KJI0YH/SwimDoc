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
        if (parameter is not int idValue)
            return;

        _entriesTable.SetAthleteId(idValue);
        _heatsTable.SetAthleteId(idValue);
        _resultsTable.SetAthleteId(idValue);
    }
}