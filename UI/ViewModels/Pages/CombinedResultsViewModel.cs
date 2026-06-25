using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EntryService;
using UI.Helpers.Threading;
using UI.Models;
using UI.Services.Navigation;

namespace UI.ViewModels.Pages;

public partial class CombinedResultsViewModel : ViewModelBase
{
    protected IAgeGroupService AgeGroupService =>
        App.Current.Services.GetRequiredService<IAgeGroupService>();
    private IEntryService EntryService =>
        App.Current.Services.GetRequiredService<IEntryService>();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private AgeGroup? _selectedAgeGroup;
    [ObservableProperty] private ObservableCollection<SearchableItem> _ageGroupOptions = new();
    [ObservableProperty] private ObservableCollection<CombinedResultsEventColumnView> _eventColumns = new();
    [ObservableProperty] private ObservableCollection<CombinedResultRow> _rows = new();
    [ObservableProperty] private CombinedResultRow? _selectedRow;
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
        await YieldToBackgroundAsync();
        await using var scope = App.Current.Services.CreateAsyncScope();
        var ageGroupService = scope.ServiceProvider.GetRequiredService<IAgeGroupService>();
        var ageGroups = await ageGroupService.Query()
            .OrderBy(ag => ag.Name)
            .ThenBy(ag => ag.Gender)
            .ThenBy(ag => ag.BirthYearMin)
            .ToListAsync()
            .ConfigureAwait(false);

        AgeGroup? target = null;
        if (_preferredAgeGroupId is int preferredId)
            target = ageGroups.FirstOrDefault(ageGroup => ageGroup.Id == preferredId);
        var selectedAgeGroup = SelectedAgeGroup;
        if (target is null && selectedAgeGroup is not null &&
            ageGroups.Any(ageGroup => ageGroup.Id == selectedAgeGroup.Id))
            target = selectedAgeGroup;
        target ??= ageGroups.FirstOrDefault();
        var preferredCleared = _preferredAgeGroupId is not null;
        if (preferredCleared)
            _preferredAgeGroupId = null;

        await DispatcherUiHelper.InvokeOnUiAsync(() =>
        {
            AgeGroupOptions = new ObservableCollection<SearchableItem>(
                ageGroups.Select(ageGroup => new SearchableItem
                {
                    Value = ageGroup,
                    DisplayText = EntityDisplayFormatter.FormatAgeGroup(ageGroup)
                }));
            SelectedAgeGroup = target;
        });
    }

    partial void OnSelectedAgeGroupChanged(AgeGroup? value) => _ = LoadRowsAsync();
    private async Task LoadRowsAsync()
    {
        if (SelectedAgeGroup?.Id is not int ageGroupId)
        {
            EventColumns = [];
            Rows = [];
            SelectedRow = null;
            return;
        }
        await DispatcherUiHelper.InvokeOnUiAsync(() => IsLoading = true);
        await YieldLoadingUiAsync();
        try
        {
            await YieldToBackgroundAsync();
            var data = await EntryService.GetCombinedResultsByAgeGroupAsync(ageGroupId).ConfigureAwait(false);
            var columns = data.EventColumns
                .Select(column => new CombinedResultsEventColumnView(column.SwimStyleId, column.Header))
                .ToList();
            var rows = BuildRows(data.Athletes);
            await DispatcherUiHelper.InvokeOnUiAsync(() =>
            {
                EventColumns = new ObservableCollection<CombinedResultsEventColumnView>(columns);
                Rows = new ObservableCollection<CombinedResultRow>(rows);
                SelectedRow = null;
            });
        }
        finally
        {
            await DispatcherUiHelper.InvokeOnUiAsync(() => IsLoading = false);
        }
    }

    partial void OnSelectedRowChanged(CombinedResultRow? value) =>
        OpenAthleteDetailsCommand.NotifyCanExecuteChanged();

    private bool CanOpenAthleteDetails() => SelectedRow?.AthleteId > 0;

    [RelayCommand(CanExecute = nameof(CanOpenAthleteDetails))]
    private void OpenAthleteDetails()
    {
        if (SelectedRow is null || SelectedRow.AthleteId <= 0)
            return;
        var navigationService = App.Current.Services.GetRequiredService<INavigationService>();
        navigationService.NavigateTo<AthleteDetailsViewModel>(SelectedRow.AthleteId);
    }

    internal static List<CombinedResultRow> BuildRows(IReadOnlyList<CombinedResultsAthleteRow> athletes)
    {
        return CombinedResultsCalculator.AssignPlaces(athletes)
            .Select(item => CreateRow(item.AthleteRow, item.Place))
            .ToList();
    }

    private static CombinedResultRow CreateRow(CombinedResultsAthleteRow athleteRow, int place)
    {
        var athlete = athleteRow.Athlete;
        return new CombinedResultRow(
            place,
            athlete.Id,
            EntityDisplayFormatter.FormatAthleteName(athlete),
            athlete.YearOfBirth.ToString(),
            EntityDisplayFormatter.FormatAthleteCategory(athlete),
            EntityDisplayFormatter.FormatAthleteClubName(athlete),
            athleteRow.TotalPoints,
            athleteRow.PointsBySwimStyleId,
            athleteRow.ScoringBySwimStyleId);
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
