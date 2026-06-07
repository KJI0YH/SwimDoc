using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using ServiceLayer.SwimStyleService;
using UI.ViewModels;
using UI.ViewModels.Pages;

namespace UI.ViewModels.Pages.Data;

public partial class ResultsByClubViewModel(IEntryService entryService) : ViewModelBase, IParticipantResultsViewModel
{
    private readonly INavigationService _navigationService =
        App.Current.Services.GetRequiredService<INavigationService>();

    private int? _clubId;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<ParticipantResultEntryView> _results = new();
    [ObservableProperty] private ParticipantResultEntryView? _selectedResult;
    public void SetClubId(int? clubId)
    {
        _clubId = clubId;
        LoadDataCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (!_clubId.HasValue)
        {
            Results = [];
            return;
        }
        IsLoading = true;
        try
        {
            var clubId = _clubId.Value;
            var eventIds = await entryService.Query()
                .Where(e => e.SwimEventId != null)
                .Where(e =>
                    (e.Athlete != null && e.Athlete.ClubId == clubId) ||
                    (e.Relay != null && e.Relay.ClubId == clubId))
                .Where(e => e.Status >= EntryStatus.FINISH)
                .Include(e => e.SwimEvent)
                .OrderBy(e => e.SwimEvent!.Order)
                .Select(e => e.SwimEventId!.Value)
                .Distinct()
                .ToListAsync();
            var rows = new List<ParticipantResultEntryView>();
            foreach (var eventId in eventIds)
            {
                var eventEntries = await entryService.GetEntriesByEventIdOrderByFinishTimeAsync(eventId);
                var eventResults = ResultsViewModel.BuildResultEntryViews(eventEntries);
                foreach (var clubResult in ResultsViewModel.FindClubResults(eventResults, clubId))
                    rows.Add(new ParticipantResultEntryView(clubResult));
            }
            Results = new ObservableCollection<ParticipantResultEntryView>(rows);
        }
        finally
        {
            IsLoading = false;
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
