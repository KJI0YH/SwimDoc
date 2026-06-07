using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.SwimStyleService;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class SwimStyleDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly EntriesBySwimStyleViewModel _entriesTable;
    private readonly EventsBySwimStyleViewModel _eventsTable;
    [ObservableProperty] private string? _title = string.Empty;
    public SwimStyleDetailsViewModel(
        IEventService eventService,
        IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService)
    {
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
    }
}
