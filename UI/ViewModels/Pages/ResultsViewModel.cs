using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class ResultsViewModel(IEventService eventService, IEntryService entryService) : 
    DataViewModel<SwimEvent, int?>(eventService)
{
    [ObservableProperty] private SwimEvent? _selectedSwimEvent;
    [ObservableProperty] private ObservableCollection<ResultEntryRowViewModel> _rows = new();

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query
            .OrderBy(se => se.Order)
            .Include(se => se.AgeGroup)
            .Include(se => se.SwimStyle);
    }

    protected override void OnItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        if (items.Count == 0)
        {
            SelectedSwimEvent = null;
            Rows = new ObservableCollection<ResultEntryRowViewModel>();
            return;
        }

        if (SelectedSwimEvent is null || items.All(e => e.Id != SelectedSwimEvent.Id))
            SelectedSwimEvent = items.OrderBy(e => e.Order).FirstOrDefault();
    }

    partial void OnSelectedSwimEventChanged(SwimEvent? value)
    {
        _ = LoadResultsForSelectedEventAsync();
    }

    private async Task LoadResultsForSelectedEventAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId)
        {
            Rows = new ObservableCollection<ResultEntryRowViewModel>();
            return;
        }

        IsLoading = true;
        try
        {
            var entries = await entryService.Query()
                .Where(e => e.SwimEventId == eventId)
                .Include(e => e.Athlete!)
                .ThenInclude(a => a.Club)
                .OrderBy(e =>
                    e.Status == EntryStatus.DNS || e.Status == EntryStatus.DNF || e.Status == EntryStatus.DSQ ? 1 : 0)
                .ThenBy(e => e.Status == EntryStatus.FINISH ? (e.FinishTime ?? int.MaxValue) : int.MaxValue)
                .ThenBy(e => e.Athlete != null ? e.Athlete.LastName : string.Empty)
                .ThenBy(e => e.Athlete != null ? e.Athlete.FirstName : string.Empty)
                .ToListAsync();

            var rows = entries
                .Select((e, idx) => new ResultEntryRowViewModel(place: idx + 1, entry: e))
                .ToList();

            Rows = new ObservableCollection<ResultEntryRowViewModel>(rows);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoToNextEvent()
    {
        if (Items.Count == 0)
            return;

        var idx = SelectedSwimEvent is null ? -1 : Items.IndexOf(SelectedSwimEvent);
        var nextIdx = Math.Min(idx + 1, Items.Count - 1);
        SelectedSwimEvent = Items[nextIdx];
    }

    [RelayCommand]
    private void GoToPrevEvent()
    {
        if (Items.Count == 0)
            return;

        var idx = SelectedSwimEvent is null ? 0 : Items.IndexOf(SelectedSwimEvent);
        if (idx < 0) idx = 0;
        var prevIdx = Math.Max(idx - 1, 0);
        SelectedSwimEvent = Items[prevIdx];
    }
}

public sealed class ResultEntryRowViewModel : ObservableObject
{
    public ResultEntryRowViewModel(int place, Entry entry)
    {
        Place = place;
        Entry = entry;
    }

    public int Place { get; }
    public Entry Entry { get; }

    public string ParticipantName => Entry.Athlete?.DisplayName ?? string.Empty;
    public string ParticipantYearOfBirth => Entry.Athlete?.YearOfBirth.ToString() ?? string.Empty;
    public string ClubName => Entry.Athlete?.DisplayClubName ?? string.Empty;

    public string ResultText => Entry.DisplayFinishTime;
    public int? Points => Entry.Points;
}

