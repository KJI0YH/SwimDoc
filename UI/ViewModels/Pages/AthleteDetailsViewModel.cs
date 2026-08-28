using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.AthleteService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.Helpers.Display;
using UI.Helpers.Threading;
using UI.Services.Navigation;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class AthleteDetailsViewModel : ViewModelBase, INavigationAware, INavigationTabState
{
    private readonly IAthleteService _athleteService;
    private readonly EntriesByAthleteViewModel _entriesTable;
    private readonly HeatsByAthleteViewModel _heatsTable;
    private readonly ResultsByAthleteViewModel _resultsTable;
    private int _titleEntityId;
    [ObservableProperty] private string? _title = string.Empty;
    [ObservableProperty] private int _selectedTabIndex;
    public int NavigationTabIndex
    {
        get => SelectedTabIndex;
        set => SelectedTabIndex = value;
    }
    public AthleteDetailsViewModel(
        IAthleteService athleteService,
        IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService,
        IEventService eventService,
        IHeatService heatService,
        INavigationService navigationService)
    {
        _athleteService = athleteService;
        _entriesTable = new EntriesByAthleteViewModel(entryService, entryDocumentReaderService);
        _heatsTable = new HeatsByAthleteViewModel(eventService, heatService, navigationService);
        _resultsTable = new ResultsByAthleteViewModel(entryService);
    }

    public ViewModelBase EntriesTable => _entriesTable;
    public ViewModelBase HeatsTable => _heatsTable;
    public ViewModelBase ResultsTable => _resultsTable;
    public void OnNavigatedTo(object? parameter)
    {
        if (NavigationContext.Parse(parameter) is not { } context || context.ResolveId() is not int athleteId)
            return;
        var focusEntryId = context.FocusEntryId;
        var focusSwimEventId = context.FocusSwimEventId;
        SelectedTabIndex = context.OpenHeatsTab ? 1 : 0;
        _entriesTable.SetAthleteId(athleteId);
        _heatsTable.SetAthleteId(athleteId, focusEntryId, focusSwimEventId);
        _resultsTable.SetAthleteId(athleteId);
        LoadTitle(athleteId);
    }

    private void LoadTitle(int id)
    {
        _titleEntityId = id;
        Title = string.Empty;
        _ = LoadTitleAsync(id);
    }

    private async Task LoadTitleAsync(int id)
    {
        var title = await DetailPageTitleLoader.LoadAthleteAsync(_athleteService, id).ConfigureAwait(false);
        await DispatcherUiHelper.InvokeOnUiAsync(() =>
        {
            if (_titleEntityId == id)
                Title = title;
        }).ConfigureAwait(false);
    }
}
