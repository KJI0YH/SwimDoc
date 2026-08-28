using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using UI.Helpers.Display;
using UI.Helpers.Threading;
using UI.Services.Navigation;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class EventDetailsViewModel : ViewModelBase, INavigationAware, INavigationTabState
{
    private readonly IEventService _eventService;
    private readonly EntriesByEventViewModel _entriesTable;
    private readonly HeatsByEventViewModel _heatsTable;
    private readonly FixationByEventViewModel _fixationTable;
    private readonly ResultsByEventViewModel _resultsTable;
    private int _titleEntityId;
    [ObservableProperty] private string? _title = string.Empty;
    [ObservableProperty] private int _selectedTabIndex;
    public int NavigationTabIndex
    {
        get => SelectedTabIndex;
        set => SelectedTabIndex = value;
    }
    public EventDetailsViewModel(
        IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService,
        IEventService eventService,
        IHeatService heatService,
        IPointScoreProvider pointScoreProvider,
        IAgeGroupService ageGroupService,
        INavigationService navigationService)
    {
        _eventService = eventService;
        _entriesTable = new EntriesByEventViewModel(entryService, entryDocumentReaderService);
        _heatsTable = new HeatsByEventViewModel(eventService, heatService, navigationService);
        _fixationTable = new FixationByEventViewModel(eventService, heatService, pointScoreProvider, navigationService);
        _resultsTable = new ResultsByEventViewModel(eventService, entryService, ageGroupService, navigationService);
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
        if (NavigationContext.Parse(parameter)?.ResolveId() is not int idValue)
            return;
        _entriesTable.SetEventId(idValue);
        _heatsTable.SetEventId(idValue);
        _fixationTable.SetEventId(idValue);
        _resultsTable.SetEventId(idValue);
        LoadTitle(idValue);
    }

    private void LoadTitle(int id)
    {
        _titleEntityId = id;
        Title = string.Empty;
        _ = LoadTitleAsync(id);
    }

    private async Task LoadTitleAsync(int id)
    {
        var title = await DetailPageTitleLoader.LoadEventAsync(_eventService, id).ConfigureAwait(false);
        await DispatcherUiHelper.InvokeOnUiAsync(() =>
        {
            if (_titleEntityId == id)
                Title = title;
        }).ConfigureAwait(false);
    }
}
