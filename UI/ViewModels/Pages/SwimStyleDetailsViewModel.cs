using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.SwimStyleService;
using UI.Helpers.Display;
using UI.Helpers.Threading;
using UI.Services.Navigation;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class SwimStyleDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly ISwimStyleService _swimStyleService;
    private readonly EntriesBySwimStyleViewModel _entriesTable;
    private readonly EventsBySwimStyleViewModel _eventsTable;
    private int _titleEntityId;
    [ObservableProperty] private string? _title = string.Empty;

    public SwimStyleDetailsViewModel(
        ISwimStyleService swimStyleService,
        IEventService eventService,
        IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService)
    {
        _swimStyleService = swimStyleService;
        _eventsTable = new EventsBySwimStyleViewModel(eventService);
        _entriesTable = new EntriesBySwimStyleViewModel(entryService, entryDocumentReaderService);
    }

    public ViewModelBase EventsTable => _eventsTable;
    public ViewModelBase EntriesTable => _entriesTable;
    public void OnNavigatedTo(object? parameter)
    {
        if (NavigationContext.Parse(parameter)?.ResolveId() is not int idValue)
            return;
        _eventsTable.SetSwimStyleId(idValue);
        _entriesTable.SetSwimStyleId(idValue);
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
        var title = await DetailPageTitleLoader.LoadSwimStyleAsync(_swimStyleService, id).ConfigureAwait(false);
        await DispatcherUiHelper.InvokeOnUiAsync(() =>
        {
            if (_titleEntityId == id)
                Title = title;
        }).ConfigureAwait(false);
    }
}
