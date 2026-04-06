using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EventService;
using ServiceLayer.SwimStyleService;
using UI.Services;
using UI.ViewModels.Generic;

namespace UI.ViewModels.Details;

public partial class EventDetailsViewModel : ViewModelBase, INavigationAware
{
    private IEventService _eventService;
    private readonly EntriesByEventViewModel _entriesTable;

    public ViewModelBase EntriesTable => _entriesTable;

    [ObservableProperty] private string? _title = string.Empty;

    public EventDetailsViewModel(
        IEventService eventService,
        IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService,
        IAgeGroupService ageGroupService,
        ISwimStyleService swimStyleService)
    {
        _eventService = eventService;
        _entriesTable = new EntriesByEventViewModel(entryService, entryDocumentReaderService);
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int idValue)
            return;

        var swimEvent = _eventService.Query().Where(e => e.Id == idValue)
            .Include(e => e.SwimStyle)
            .Include(e => e.AgeGroup)
            .FirstOrDefault();
        Title = swimEvent?.DisplayName;

        _entriesTable.SetEventId(idValue);
    }
}