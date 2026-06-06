using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using UI.Resources;

namespace UI.Models.Imports;

public partial class EntriesFile : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AthletesStatsDisplay))]
    private int _athletesAdded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AthletesStatsDisplay))]
    private int _athletesScanned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AthletesStatsDisplay))]
    private int _athletesUpdated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AthletesStatsDisplay))]
    private int _athletesWithErrors;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClubsStatsDisplay))]
    private int _clubsAdded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClubsStatsDisplay))]
    private int _clubsScanned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClubsStatsDisplay))]
    private int _clubsUpdated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClubsStatsDisplay))]
    private int _clubsWithErrors;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EntriesStatsDisplay))]
    private int _entriesAdded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EntriesStatsDisplay))]
    private int _entriesScanned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EntriesStatsDisplay))]
    private int _entriesUpdated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EntriesStatsDisplay))]
    private int _entriesWithErrors;

    [ObservableProperty] private IReadOnlyList<string> _errors = Array.Empty<string>();
    [ObservableProperty] private int _errorsCount;

    [ObservableProperty] private string _fileName;
    [ObservableProperty] private string _fullPath;

    [ObservableProperty] private bool _isDetailsOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _isSummaryRow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private ImportFileStatus _status = ImportFileStatus.Pending;

    [ObservableProperty] private IReadOnlyList<string> _warnings = Array.Empty<string>();
    [ObservableProperty] private int _warningsCount;

    public string StatusText =>
        IsSummaryRow ? string.Empty : Strings.GetEnumDisplay(Status);

    public string ClubsStatsDisplay => FormatPersistedCount(ClubsAdded, ClubsUpdated);

    public string AthletesStatsDisplay => FormatPersistedCount(AthletesAdded, AthletesUpdated);

    public string EntriesStatsDisplay => FormatPersistedCount(EntriesAdded, EntriesUpdated);

    public EntriesFile(string fileName, string fullPath)
    {
        FileName = fileName;
        FullPath = fullPath;
    }

    private static string FormatPersistedCount(int added, int updated) =>
        (added + updated).ToString(CultureInfo.CurrentCulture);
}
