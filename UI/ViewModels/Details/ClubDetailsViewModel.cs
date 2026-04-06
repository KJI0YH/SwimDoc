using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using UI.Services;
using UI.ViewModels.Generic;

namespace UI.ViewModels.Details;

public partial class ClubDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly IClubService _clubService;
    private readonly AthletesByClubViewModel _athletesTable;
    private readonly EntriesByClubViewModel _entriesTable;

    public ViewModelBase AthletesTable => _athletesTable;
    public ViewModelBase EntriesTable => _entriesTable;

    [ObservableProperty] private string? _title = string.Empty;

    public ClubDetailsViewModel(
        IClubService clubService,
        IAthleteService athleteService,
        IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService)
    {
        _clubService = clubService;
        _athletesTable = new AthletesByClubViewModel(athleteService);
        _entriesTable = new EntriesByClubViewModel(entryService, entryDocumentReaderService);
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int idValue)
            return;

        var club = _clubService.FindAsync(idValue).Result;
        Title = club?.Name;

        _athletesTable.SetClubId(club.Id);
        _entriesTable.SetClubId(club.Id);
    }
}