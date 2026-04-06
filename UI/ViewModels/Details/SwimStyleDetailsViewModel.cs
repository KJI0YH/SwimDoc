using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.EntryService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EventService;
using ServiceLayer.SwimStyleService;
using UI.Services;
using UI.ViewModels.Generic;

namespace UI.ViewModels.Details;

public partial class SwimStyleDetailsViewModel : ViewModelBase, INavigationAware
{
    private ISwimStyleService _swimStyleService;
    private readonly EventsBySwimStyleViewModel _eventsTable;
    private readonly EntriesBySwimStyleViewModel _entriesTable;
    
    public ViewModelBase EventsTable => _eventsTable;
    public ViewModelBase EntriesTable => _entriesTable;

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

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int idValue)
            return;
        
        var swimStyle = _swimStyleService.FindAsync(idValue).Result;
        Title = swimStyle?.DisplayName;
        
        _eventsTable.SetSwimStyleId(idValue);
        _entriesTable.SetSwimStyleId(idValue);
    }
}

