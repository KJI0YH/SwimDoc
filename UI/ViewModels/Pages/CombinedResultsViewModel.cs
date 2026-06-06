using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EntryService;
using UI.Helpers;
using UI.Views.Controls.SearchableComboBox;

namespace UI.ViewModels.Pages;

public partial class CombinedResultsViewModel(
    IAgeGroupService ageGroupService,
    IEntryService entryService) : ViewModelBase
{
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private AgeGroup? _selectedAgeGroup;
    [ObservableProperty] private ObservableCollection<SearchableItem> _ageGroupOptions = new();
    [ObservableProperty] private ObservableCollection<CombinedResultsEventColumnView> _eventColumns = new();
    [ObservableProperty] private ObservableCollection<CombinedResultRow> _rows = new();

    public virtual bool ShowAgeGroupSelector => true;

    private int? _preferredAgeGroupId;

    public void SetPreferredAgeGroupId(int? ageGroupId) => _preferredAgeGroupId = ageGroupId;

    public void TrySelectAgeGroup(int ageGroupId)
    {
        if (AgeGroupOptions.FirstOrDefault(item => item.Value is AgeGroup ag && ag.Id == ageGroupId)?.Value
            is AgeGroup ageGroup)
        {
            if (SelectedAgeGroup?.Id != ageGroupId)
                SelectedAgeGroup = ageGroup;
            return;
        }

        _preferredAgeGroupId = ageGroupId;
    }

    public async Task InitializeAsync()
    {
        var ageGroups = await ageGroupService.Query()
            .OrderBy(ag => ag.Name)
            .ThenBy(ag => ag.Gender)
            .ThenBy(ag => ag.BirthYearMin)
            .ToListAsync();

        AgeGroupOptions = new ObservableCollection<SearchableItem>(
            ageGroups.Select(ageGroup => new SearchableItem
            {
                Value = ageGroup,
                DisplayText = EntityDisplayFormatter.FormatAgeGroup(ageGroup)
            }));

        AgeGroup? target = null;
        if (_preferredAgeGroupId is int preferredId)
            target = ageGroups.FirstOrDefault(ageGroup => ageGroup.Id == preferredId);

        if (target is null && SelectedAgeGroup is not null &&
            ageGroups.Any(ageGroup => ageGroup.Id == SelectedAgeGroup.Id))
            target = SelectedAgeGroup;

        SelectedAgeGroup = target ?? ageGroups.FirstOrDefault();
        _preferredAgeGroupId = null;
    }

    partial void OnSelectedAgeGroupChanged(AgeGroup? value) => _ = LoadRowsAsync();

    private async Task LoadRowsAsync()
    {
        if (SelectedAgeGroup?.Id is not int ageGroupId)
        {
            EventColumns = [];
            Rows = [];
            return;
        }

        IsLoading = true;
        try
        {
            var data = await entryService.GetCombinedResultsByAgeGroupAsync(ageGroupId);
            EventColumns = new ObservableCollection<CombinedResultsEventColumnView>(
                data.EventColumns.Select(column => new CombinedResultsEventColumnView(column.EventId, column.Header)));
            Rows = new ObservableCollection<CombinedResultRow>(BuildRows(data.Athletes));
        }
        finally
        {
            IsLoading = false;
        }
    }

    internal static List<CombinedResultRow> BuildRows(IReadOnlyList<CombinedResultsAthleteRow> athletes)
    {
        if (athletes.Count == 0)
            return [];

        var rows = new List<CombinedResultRow>();
        var officialAthletes = athletes.Where(row => row.IsInOfficialStandings).ToList();
        var place = 1;
        var previousTotal = officialAthletes.Count > 0 ? officialAthletes[0].TotalPoints : 0;

        foreach (var (athleteRow, index) in officialAthletes.Select((row, index) => (row, index)))
        {
            if (index > 0 && athleteRow.TotalPoints != previousTotal)
            {
                place = index + 1;
                previousTotal = athleteRow.TotalPoints;
            }

            rows.Add(CreateRow(athleteRow, place, isOutOfScoring: false));
        }

        foreach (var athleteRow in athletes.Where(row => !row.IsInOfficialStandings))
            rows.Add(CreateRow(athleteRow, place: null, isOutOfScoring: true));

        return rows;
    }

    private static CombinedResultRow CreateRow(CombinedResultsAthleteRow athleteRow, int? place, bool isOutOfScoring)
    {
        var athlete = athleteRow.Athlete;
        return new CombinedResultRow(
            place,
            athlete.Id,
            athlete.DisplayName,
            athlete.YearOfBirth.ToString(),
            athlete.DisplayClubName,
            athleteRow.TotalPoints,
            athleteRow.PointsByEventId,
            athleteRow.ScoringByEventId,
            isOutOfScoring);
    }

    [RelayCommand]
    private void GoToNextAgeGroup()
    {
        if (AgeGroupOptions.Count == 0)
            return;

        var currentIndex = SelectedAgeGroup is null
            ? -1
            : AgeGroupOptions.ToList().FindIndex(item => item.Value is AgeGroup ag && ag.Id == SelectedAgeGroup.Id);
        var nextIndex = Math.Min(currentIndex + 1, AgeGroupOptions.Count - 1);
        if (AgeGroupOptions[nextIndex].Value is AgeGroup nextAgeGroup)
            SelectedAgeGroup = nextAgeGroup;
    }

    [RelayCommand]
    private void GoToPrevAgeGroup()
    {
        if (AgeGroupOptions.Count == 0)
            return;

        var currentIndex = SelectedAgeGroup is null
            ? 0
            : AgeGroupOptions.ToList().FindIndex(item => item.Value is AgeGroup ag && ag.Id == SelectedAgeGroup.Id);
        if (currentIndex < 0)
            currentIndex = 0;

        var prevIndex = Math.Max(currentIndex - 1, 0);
        if (AgeGroupOptions[prevIndex].Value is AgeGroup prevAgeGroup)
            SelectedAgeGroup = prevAgeGroup;
    }
}

public sealed record CombinedResultsEventColumnView(int EventId, string Header);
