using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using UI.Services;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class ClubDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly AthletesByClubViewModel _athletesTable;
    private readonly IClubService _clubService;
    private readonly EntriesByClubViewModel _entriesTable;
    private readonly ResultsByClubViewModel _resultsTable;

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
        _resultsTable = new ResultsByClubViewModel(entryService);
    }

    public ViewModelBase AthletesTable => _athletesTable;
    public ViewModelBase EntriesTable => _entriesTable;
    public ViewModelBase ResultsTable => _resultsTable;

    public void OnNavigatedTo(object? parameter)
    {
        if (NavigationContext.Parse(parameter)?.ResolveId() is not int idValue)
            return;

        _athletesTable.SetClubId(idValue);
        _entriesTable.SetClubId(idValue);
        _resultsTable.SetClubId(idValue);
    }
}
