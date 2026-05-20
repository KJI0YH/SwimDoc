using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using UI.Services;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class EventDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly EntriesByEventViewModel _entriesTable;
    private readonly HeatsByEventViewModel _heatsTable;
    private readonly FixationByEventViewModel _fixationTable;
    private readonly ResultsByEventViewModel _resultsTable;

    [ObservableProperty] private string? _title = string.Empty;

    public EventDetailsViewModel(
        IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService,
        IEventService eventService,
        IHeatService heatService,
        IPointScoreProvider pointScoreProvider)
    {
        _entriesTable = new EntriesByEventViewModel(entryService, entryDocumentReaderService);
        _heatsTable = new HeatsByEventViewModel(eventService, heatService);
        _fixationTable = new FixationByEventViewModel(eventService, heatService, pointScoreProvider);
        _resultsTable = new ResultsByEventViewModel(eventService, entryService);

        _fixationTable.EventResultsChanged += eventId =>
        {
            _ = _resultsTable.RefreshForEventAsync(eventId);
            _ = _heatsTable.RefreshAsync();
        };
    }

    public ViewModelBase EntriesTable => _entriesTable;

    public ViewModelBase HeatsTable => _heatsTable;

    public ViewModelBase FixationTable => _fixationTable;

    public ViewModelBase ResultsTable => _resultsTable;

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int idValue)
            return;

        _entriesTable.SetEventId(idValue);
        _heatsTable.SetEventId(idValue);
        _fixationTable.SetEventId(idValue);
        _resultsTable.SetEventId(idValue);
    }
}