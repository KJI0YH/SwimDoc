using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EntryService;
using UI.Helpers.Threading;
using UI.ViewModels;
using UI.ViewModels.Pages;

namespace UI.ViewModels.Pages.Data;

public partial class ResultsByAthleteViewModel(IEntryService entryService) : ViewModelBase, IParticipantResultsViewModel
{
    private readonly INavigationService _navigationService =
        App.Current.Services.GetRequiredService<INavigationService>();

    private int? _athleteId;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<ParticipantResultEntryView> _results = new();
    [ObservableProperty] private ParticipantResultEntryView? _selectedResult;

    public void SetAthleteId(int? athleteId)
    {
        _athleteId = athleteId;
        LoadDataCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (!_athleteId.HasValue)
        {
            Results = [];
            return;
        }

        await DispatcherUiHelper.InvokeOnUiAsync(() => IsLoading = true);
        await YieldLoadingUiAsync();
        try
        {
            var athleteId = _athleteId.Value;
            await YieldToBackgroundAsync();

            await using var scope = App.Current.Services.CreateAsyncScope();
            var scopedEntryService = scope.ServiceProvider.GetRequiredService<IEntryService>();
            var eventIds = await scopedEntryService.Query()
                .Where(e => e.SwimEventId != null)
                .Where(e =>
                    e.AthleteId == athleteId ||
                    (e.Relay != null && e.Relay.Positions.Any(p => p.AthleteId == athleteId)))
                .Where(e => e.Status >= EntryStatus.FINISH)
                .Include(e => e.SwimEvent)
                .OrderBy(e => e.SwimEvent!.Order)
                .Select(e => e.SwimEventId!.Value)
                .Distinct()
                .ToListAsync()
                .ConfigureAwait(false);

            var rows = new List<ParticipantResultEntryView>();
            foreach (var eventId in eventIds)
            {
                var eventEntries = await scopedEntryService
                    .GetEntriesByEventIdOrderByFinishTimeAsync(eventId)
                    .ConfigureAwait(false);
                var eventResults = ResultsViewModel.BuildResultEntryViews(eventEntries);
                var athleteResult = ResultsViewModel.FindAthleteResult(eventResults, athleteId);
                if (athleteResult is not null)
                    rows.Add(new ParticipantResultEntryView(athleteResult, athleteId));
            }

            await DispatcherUiHelper.InvokeOnUiAsync(() =>
                Results = new ObservableCollection<ParticipantResultEntryView>(rows));
        }
        finally
        {
            await DispatcherUiHelper.InvokeOnUiAsync(() => IsLoading = false);
        }
    }

    partial void OnSelectedResultChanged(ParticipantResultEntryView? value) =>
        OpenEventResultsCommand.NotifyCanExecuteChanged();

    private bool CanOpenEventResults() => SelectedResult?.Entry.SwimEventId is not null;

    [RelayCommand(CanExecute = nameof(CanOpenEventResults))]
    private void OpenEventResults()
    {
        if (SelectedResult?.Entry.SwimEventId is not int eventId)
            return;
        _navigationService.NavigateTo<ResultsViewModel>(eventId);
    }
}
