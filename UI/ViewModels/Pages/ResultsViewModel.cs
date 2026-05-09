using System.Collections.ObjectModel;
using System.Windows.Documents;
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
    [ObservableProperty] private ObservableCollection<ResultEntryView> _entries = new();

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
            Entries = [];
            return;
        }

        SelectedSwimEvent ??= items.OrderBy(e => e.Order).FirstOrDefault();
    }

    partial void OnSelectedSwimEventChanged(SwimEvent? value)
    {
        _ = LoadEntriesAsync();
    }

    private async Task LoadEntriesAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId)
        {
            Entries = [];
            return;
        }

        IsLoading = true;
        try
        {
            var entries = await entryService.GetEntriesByEventIdOrderByFinishTimeAsync(eventId);

            var place = 1;
            List<ResultEntryView> orderedEntries = [new(place++, entries.First())];
            var prevResult = orderedEntries.Last();
            foreach (var entry in entries.Skip(1))
            {
                orderedEntries.Add(entry.FinishTime == prevResult.Entry.FinishTime
                    ? new ResultEntryView(prevResult.Place, entry)
                    : new ResultEntryView(place, entry));
                prevResult = orderedEntries.Last();
                place++;
            }

            Entries = new ObservableCollection<ResultEntryView>(orderedEntries);
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

public sealed class ResultEntryView(int place, Entry entry) : ObservableObject
{
    public int Place { get; } = place;
    public Entry Entry { get; } = entry;

    public string ParticipantName => Entry.Athlete?.DisplayName ?? string.Empty;
    public string ParticipantYearOfBirth => Entry.Athlete?.YearOfBirth.ToString() ?? string.Empty;
    public string ClubName => Entry.Athlete?.DisplayClubName ?? string.Empty;

    public string ResultText => Entry.DisplayFinishTime;
    public int? Points => Entry.Points;
}