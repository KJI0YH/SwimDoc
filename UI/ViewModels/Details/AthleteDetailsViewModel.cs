using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryService;
using ServiceLayer.EntryDocumentReaderService;
using UI.Services;
using UI.ViewModels.Generic;

namespace UI.ViewModels.Details;

public partial class AthleteDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly IAthleteService _athleteService;
    private readonly EntriesByAthleteViewModel _entriesTable;
    [ObservableProperty] private string? _title = string.Empty;

    public ViewModelBase EntriesTable => _entriesTable;

    public AthleteDetailsViewModel(
        IAthleteService athleteService,
        IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService,
        IClubService clubService)
    {
        _athleteService = athleteService;
        _entriesTable = new EntriesByAthleteViewModel(entryService, entryDocumentReaderService);
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int idValue)
            return;

        var athlete = _athleteService.FindAsync(idValue).Result;

        if (athlete == null)
            return;

        Title = athlete.DisplayName;

        _entriesTable.SetAthleteId(athlete.Id);
    }
}