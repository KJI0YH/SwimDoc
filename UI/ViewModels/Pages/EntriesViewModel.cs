using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using BizLogic.EntryDocumentReaderLogic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using UI.Helpers;
using UI.Resources;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.ViewModels.Windows.LoadEntriesFromPreviousEvent;
using UI.Views.Windows.AddEdit;
using UI.Views.Windows.LoadEntriesFromPreviousEvent;
using QueryableSortByDirection = UI.ViewModels.Generic.QueryableSortByDirection;

namespace UI.ViewModels.Pages;

public partial class EntriesViewModel(
    IEntryService entryService,
    IEntryDocumentReaderService entryDocumentReaderService)
    : DataViewModel<Entry, int?>(entryService), INavigationAware
{
    protected override PagingPage PagingSettingsPage => PagingPage.Entries;

    private const int SlowestTimeRank = int.MaxValue;
    private bool _filterOptionsInitialized;
    private bool _cultureSubscribed;

    [ObservableProperty] private ObservableCollection<EventFilterOption<EventRound>> _roundFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<int>> _distanceFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<Stroke>> _strokeFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<Gender>> _genderFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<EntryStatus>> _statusFilterOptions = new();
    [ObservableProperty] private bool _isFiltersPanelVisible;

    public string RoundFilterText => GetFilterText(RoundFilterOptions, Strings.Filters_Round);
    public string DistanceFilterText => GetFilterText(DistanceFilterOptions, Strings.Filters_Distance);
    public string StrokeFilterText => GetFilterText(StrokeFilterOptions, Strings.Filters_Stroke);
    public string GenderFilterText => GetFilterText(GenderFilterOptions, Strings.Filters_Gender);
    public string StatusFilterText => GetFilterText(StatusFilterOptions, Strings.Filters_Status);

    public enum EntriesImportBarKind
    {
        File,
        Event
    }

    public enum ImportFileStatus
    {
        Summary,
        Pending,
        Processing,
        Completed,
        CompletedWithWarnings,
        Failed,
        Canceled
    }

    private readonly IAddEditWindowFactory _windowFactory =
        App.Current.Services.GetRequiredService<IAddEditWindowFactory>();

    private CancellationTokenSource? _importCts;
    private volatile bool _suppressImportUiUpdates;
    [ObservableProperty] private ObservableCollection<EntriesFile> _importFiles = new();
    [ObservableProperty] private ICollectionView? _importFilesView;
    [ObservableProperty] private string _importHeader = string.Empty;
    [ObservableProperty] private string _importMessage = string.Empty;
    [ObservableProperty] private int _importProcessedFiles;
    [ObservableProperty] private int _importSummaryAthletesAdded;
    [ObservableProperty] private int _importSummaryAthletesUpdated;

    [ObservableProperty] private int _importSummaryClubsAdded;
    [ObservableProperty] private int _importSummaryClubsUpdated;
    [ObservableProperty] private int _importSummaryEntriesAdded;
    [ObservableProperty] private int _importSummaryEntriesUpdated;
    [ObservableProperty] private int _importSummaryErrorsCount;
    [ObservableProperty] private int _importSummaryFilesCount;
    [ObservableProperty] private int _importSummaryWarningsCount;
    [ObservableProperty] private int _importTotalFiles;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsFileImportBar), nameof(IsEventImportBar))]
    private EntriesImportBarKind _importBarKind = EntriesImportBarKind.File;

    [ObservableProperty] private bool _isImportBarOpen;
    [ObservableProperty] private bool _isImportDetailsOpen;
    [ObservableProperty] private bool _isImportRunning;

    [ObservableProperty] private int _eventImportCreatedCount;
    [ObservableProperty] private ObservableCollection<string> _eventImportErrors = new();
    [ObservableProperty] private string _eventImportPreviousEventName = string.Empty;
    [ObservableProperty] private string _eventImportTargetEventName = string.Empty;

    public bool IsFileImportBar => ImportBarKind == EntriesImportBarKind.File;
    public bool IsEventImportBar => ImportBarKind == EntriesImportBarKind.Event;
    public bool HasEventImportDetails => EventImportErrors.Count > 0;

    private EntriesFile? _summaryRow;

    private void RecalculateImportSummary()
    {
        var files = ImportFiles;
        var dataFiles = Enumerable.Where<EntriesFile>(files, f => !f.IsSummaryRow).ToList();

        ImportSummaryFilesCount = dataFiles.Count;
        ImportSummaryClubsAdded = dataFiles.Sum(f => f.ClubsAdded);
        ImportSummaryClubsUpdated = dataFiles.Sum(f => f.ClubsUpdated);
        ImportSummaryAthletesAdded = dataFiles.Sum(f => f.AthletesAdded);
        ImportSummaryAthletesUpdated = dataFiles.Sum(f => f.AthletesUpdated);
        ImportSummaryEntriesAdded = dataFiles.Sum(f => f.EntriesAdded);
        ImportSummaryEntriesUpdated = dataFiles.Sum(f => f.EntriesUpdated);
        ImportSummaryWarningsCount = dataFiles.Sum(f => f.WarningsCount);
        ImportSummaryErrorsCount = dataFiles.Sum(f => f.ErrorsCount);

        _summaryRow ??= new EntriesFile(Strings.Import_Summary_Total, string.Empty) { IsSummaryRow = true };
        if (!files.Contains(_summaryRow))
            files.Add(_summaryRow);

        _summaryRow.FileName = Strings.Import_Summary_Total;
        _summaryRow.FullPath = string.Format(Strings.Import_Summary_FilesCountFormat, ImportSummaryFilesCount);
        _summaryRow.ClubsScanned = dataFiles.Sum(f => f.ClubsScanned);
        _summaryRow.ClubsWithErrors = dataFiles.Sum(f => f.ClubsWithErrors);
        _summaryRow.ClubsAdded = ImportSummaryClubsAdded;
        _summaryRow.ClubsUpdated = ImportSummaryClubsUpdated;
        _summaryRow.AthletesScanned = dataFiles.Sum(f => f.AthletesScanned);
        _summaryRow.AthletesWithErrors = dataFiles.Sum(f => f.AthletesWithErrors);
        _summaryRow.AthletesAdded = ImportSummaryAthletesAdded;
        _summaryRow.AthletesUpdated = ImportSummaryAthletesUpdated;
        _summaryRow.EntriesScanned = dataFiles.Sum(f => f.EntriesScanned);
        _summaryRow.EntriesWithErrors = dataFiles.Sum(f => f.EntriesWithErrors);
        _summaryRow.EntriesAdded = ImportSummaryEntriesAdded;
        _summaryRow.EntriesUpdated = ImportSummaryEntriesUpdated;
        _summaryRow.WarningsCount = ImportSummaryWarningsCount;
        _summaryRow.ErrorsCount = ImportSummaryErrorsCount;
        _summaryRow.Warnings = Array.Empty<string>();
        _summaryRow.Errors = Array.Empty<string>();
        _summaryRow.IsDetailsOpen = false;
        _summaryRow.Status = ImportFileStatus.Pending;

        if (ImportFilesView is ListCollectionView listView)
            listView.Refresh();
    }

    partial void OnImportFilesChanged(ObservableCollection<EntriesFile> value)
    {
        ImportFilesView = ImportFilesViewHelper.CreateView(value);
    }

    protected override void InitializeColumns()
    {
        EnsureFilterOptionsInitialized();
        InitializeEntriesColumns();
    }

    protected void InitializeEntriesColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(new ColumnConfiguration<Entry>(".", Strings.Entries_Col_Distance, 380,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.SwimEvent != null ? e.SwimEvent.Order : int.MaxValue)
                    : query.OrderByDescending(e => e.SwimEvent != null ? e.SwimEvent.Order : int.MaxValue);
            })
        {
            Converter = EntityDisplayConverter.Instance,
            ConverterParameter = EntityDisplayConverter.EntrySwimKind
        });
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("DisplayParticipantName", Strings.Entries_Col_Participant, 250,
            (query, direction) => QueryableSortByDirection.Sort(query, direction,
                q => Queryable
                    .OrderBy<Entry, string>(q,
                        e => e.Athlete != null ? e.Athlete.LastName : e.Relay != null ? e.Relay.Club.Name : null)
                    .ThenBy(e => e.Athlete != null ? e.Athlete.FirstName : null)
                    .ThenBy(e => e.Id),
                q => Queryable
                    .OrderByDescending<Entry, string>(q,
                        e => e.Athlete != null ? e.Athlete.LastName : e.Relay != null ? e.Relay.Club.Name : null)
                    .ThenByDescending(e => e.Athlete != null ? e.Athlete.FirstName : null)
                    .ThenByDescending(e => e.Id))));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("DisplayParticipantBirthYear", Strings.Athletes_Col_BirthYear, 120,
            (query, direction) => QueryableSortByDirection.Sort(query, direction,
                q => Queryable
                    .OrderBy<Entry, int>(q, e => e.Athlete != null ? e.Athlete.YearOfBirth : int.MaxValue)
                    .ThenBy(e => e.Id),
                q => Queryable
                    .OrderByDescending<Entry, int>(q, e => e.Athlete != null ? e.Athlete.YearOfBirth : int.MinValue)
                    .ThenByDescending(e => e.Id))));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("DisplayParticipantClubName", Strings.Entries_Col_Team, 200,
            (query, direction) => QueryableSortByDirection.Sort(query, direction,
                q => Queryable
                    .OrderBy<Entry, string>(q,
                        e => e.Athlete != null ? e.Athlete.Club.Name : e.Relay != null ? e.Relay.Club.Name : null)
                    .ThenBy(e => e.Id),
                q => Queryable
                    .OrderByDescending<Entry, string>(q,
                        e => e.Athlete != null ? e.Athlete.Club.Name : e.Relay != null ? e.Relay.Club.Name : null)
                    .ThenByDescending(e => e.Id))));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("Status", Strings.Entries_Col_Status, 150));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("DisplayEntryTime", Strings.Entries_Col_EntryTime, 95,
            (query, direction) =>
                direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.EntryTime ?? SlowestTimeRank).ThenBy(e => e.Id)
                    : query.OrderByDescending(e => e.EntryTime ?? SlowestTimeRank).ThenByDescending(e => e.Id),
            nameof(Entry.EntryTime)));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("DisplayFinishTime", Strings.Entries_Col_FinishTime, 95 ,
            (query, direction) =>
                direction == ListSortDirection.Ascending
                    ? query
                        .OrderBy(e =>
                            e.Status == EntryStatus.FINISH && e.FinishTime.HasValue
                                ? e.FinishTime!.Value
                                : SlowestTimeRank)
                        .ThenBy(e => e.Id)
                    : query
                        .OrderByDescending(e =>
                            e.Status == EntryStatus.FINISH && e.FinishTime.HasValue
                                ? e.FinishTime!.Value
                                : SlowestTimeRank)
                        .ThenByDescending(e => e.Id),
            nameof(Entry.FinishTime)));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("Points", Strings.Entries_Col_Points, 50));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("Comment", Strings.Entries_Col_Comment, 250));
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        return query
            .Include(entry => entry.Athlete)
            .ThenInclude(a => a.Club)
            .Include(entry => entry.Relay)
            .ThenInclude(relay => relay.Club)
            .Include(entry => entry.Relay)
            .ThenInclude(relay => relay.Positions)
            .ThenInclude(pos => pos.Athlete)
            .Include(entry => entry.SwimStyle)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(se => se.SwimStyle)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(se => se.AgeGroup)
            .Include(entry => entry.HeatPosition)
            .ThenInclude(heatPosition => heatPosition.Heat);
    }

    protected override void OpenEntryDetails(Entry entry)
    {
        if (!EntryAthleteNavigationHelper.TryGetAthleteId(entry, out var athleteId))
            return;

        if (entry.HeatPosition is not null)
        {
            NavigationService.NavigateTo<AthleteDetailsViewModel>(new AthleteDetailsNavigationParameter
            {
                AthleteId = athleteId,
                FocusEntryId = entry.Id,
                FocusSwimEventId = entry.SwimEventId
            });
            return;
        }

        NavigationService.NavigateTo<AthleteDetailsViewModel>(athleteId);
    }

    protected override IQueryable<Entry> ApplySearch(IQueryable<Entry> query)
    {
        query = ApplySelectedFilters(query);

        if (string.IsNullOrWhiteSpace(SearchText))
            return query;

        var terms = SearchText.Trim()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0)
            return query;

        return terms.Aggregate(query, (current, term) =>
            current.Where(e =>
                (e.Athlete != null && (
                    SwimDocDbFunctions.ContainsIgnoreCase(e.Athlete.FirstName, term) ||
                    SwimDocDbFunctions.ContainsIgnoreCase(e.Athlete.LastName, term))) ||
                (e.Relay != null && e.Relay.Club != null &&
                 SwimDocDbFunctions.ContainsIgnoreCase(e.Relay.Club.Name, term))));
    }

    private IQueryable<Entry> ApplySelectedFilters(IQueryable<Entry> query)
    {
        var rounds = RoundFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (rounds.Length > 0)
            query = query.Where(e => e.SwimEvent != null && rounds.Contains(e.SwimEvent.Round));

        var distances = DistanceFilterOptions.Where(option => option.IsSelected).Select(option => option.Value)
            .ToArray();
        if (distances.Length > 0)
            query = query.Where(e => distances.Contains(e.SwimStyle.Distance));

        var strokes = StrokeFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (strokes.Length > 0)
            query = query.Where(e => strokes.Contains(e.SwimStyle.Stroke));

        var genders = GenderFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (genders.Length > 0)
            query = query.Where(e =>
                (e.SwimEvent != null && genders.Contains(e.SwimEvent.AgeGroup.Gender)) ||
                (e.Athlete != null && genders.Contains(e.Athlete.Gender)));

        var statuses = StatusFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (statuses.Length > 0)
            query = query.Where(e => statuses.Contains(e.Status));

        return query;
    }

    protected void EnsureFilterOptionsInitialized()
    {
        if (_filterOptionsInitialized)
            return;

        _filterOptionsInitialized = true;
        InitializeFilterOptions();
        SubscribeFilterOptions();
        EnsureCultureSubscription();
    }

    private void EnsureCultureSubscription()
    {
        if (_cultureSubscribed)
            return;

        _cultureSubscribed = true;
        App.Current.Services.GetRequiredService<ILocalizationService>().CultureChanged += OnCultureChanged;
    }

    private void OnCultureChanged(CultureInfo culture)
    {
        if (!_filterOptionsInitialized)
            return;

        RefreshLocalizedEnumFilterOptions();
        ReloadFromFirstPage();
    }

    private void RefreshLocalizedEnumFilterOptions()
    {
        foreach (var option in RoundFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);
        foreach (var option in StrokeFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);
        foreach (var option in GenderFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);
        foreach (var option in StatusFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);

        OnPropertyChanged(nameof(RoundFilterText));
        OnPropertyChanged(nameof(StrokeFilterText));
        OnPropertyChanged(nameof(GenderFilterText));
        OnPropertyChanged(nameof(StatusFilterText));
    }

    protected void ResetFilterOptions()
    {
        UnsubscribeFilterOptions(RoundFilterOptions);
        UnsubscribeFilterOptions(DistanceFilterOptions);
        UnsubscribeFilterOptions(StrokeFilterOptions);
        UnsubscribeFilterOptions(GenderFilterOptions);
        UnsubscribeFilterOptions(StatusFilterOptions);
        _filterOptionsInitialized = false;
    }

    protected virtual IQueryable<Entry> GetFilterOptionsSource() => _crudService.Query();

    protected virtual void InitializeFilterOptions()
    {
        var distances = GetFilterOptionsSource()
            .Select(e => e.SwimStyle.Distance)
            .Distinct()
            .OrderBy(distance => distance)
            .ToList();

        DistanceFilterOptions = new ObservableCollection<EventFilterOption<int>>(
            distances.Select(distance => new EventFilterOption<int>(
                distance,
                string.Format(Strings.Distance_MetersFormat, distance))));

        RoundFilterOptions = new ObservableCollection<EventFilterOption<EventRound>>(
            Enum.GetValues<EventRound>().Select(round =>
                new EventFilterOption<EventRound>(round, Strings.GetEnumDisplay(round))));

        StrokeFilterOptions = new ObservableCollection<EventFilterOption<Stroke>>(
            Enum.GetValues<Stroke>().Select(stroke =>
                new EventFilterOption<Stroke>(stroke, Strings.GetEnumDisplay(stroke))));

        GenderFilterOptions = new ObservableCollection<EventFilterOption<Gender>>(
            Enum.GetValues<Gender>().Select(gender =>
                new EventFilterOption<Gender>(gender, Strings.GetEnumDisplay(gender))));

        StatusFilterOptions = new ObservableCollection<EventFilterOption<EntryStatus>>(
            Enum.GetValues<EntryStatus>().Select(status =>
                new EventFilterOption<EntryStatus>(status, Strings.GetEnumDisplay(status))));
    }

    private void SubscribeFilterOptions()
    {
        SubscribeFilterOptions(RoundFilterOptions);
        SubscribeFilterOptions(DistanceFilterOptions);
        SubscribeFilterOptions(StrokeFilterOptions);
        SubscribeFilterOptions(GenderFilterOptions);
        SubscribeFilterOptions(StatusFilterOptions);
    }

    private void SubscribeFilterOptions<T>(IEnumerable<EventFilterOption<T>> options)
    {
        foreach (var option in options)
            option.PropertyChanged += OnFilterOptionPropertyChanged;
    }

    private void UnsubscribeFilterOptions<T>(IEnumerable<EventFilterOption<T>> options)
    {
        foreach (var option in options)
            option.PropertyChanged -= OnFilterOptionPropertyChanged;
    }

    private void OnFilterOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IEventFilterOption.IsSelected))
            return;

        OnPropertyChanged(nameof(RoundFilterText));
        OnPropertyChanged(nameof(DistanceFilterText));
        OnPropertyChanged(nameof(StrokeFilterText));
        OnPropertyChanged(nameof(GenderFilterText));
        OnPropertyChanged(nameof(StatusFilterText));
        ClearFiltersCommand.NotifyCanExecuteChanged();
        ReloadFromFirstPage();
    }

    [RelayCommand]
    private void ToggleFiltersPanel() => IsFiltersPanelVisible = !IsFiltersPanelVisible;

    [RelayCommand(CanExecute = nameof(HasActiveFilters))]
    private void ClearFilters()
    {
        ClearFilterOptions(RoundFilterOptions);
        ClearFilterOptions(DistanceFilterOptions);
        ClearFilterOptions(StrokeFilterOptions);
        ClearFilterOptions(GenderFilterOptions);
        ClearFilterOptions(StatusFilterOptions);
    }

    private bool HasActiveFilters() =>
        RoundFilterOptions.Any(option => option.IsSelected) ||
        DistanceFilterOptions.Any(option => option.IsSelected) ||
        StrokeFilterOptions.Any(option => option.IsSelected) ||
        GenderFilterOptions.Any(option => option.IsSelected) ||
        StatusFilterOptions.Any(option => option.IsSelected);

    private static void ClearFilterOptions<T>(IEnumerable<EventFilterOption<T>> options)
    {
        foreach (var option in options)
            option.IsSelected = false;
    }

    private void ReloadFromFirstPage()
    {
        if (CurrentPage == 0)
            LoadDataCommand.Execute(null);
        else
            CurrentPage = 0;
    }

    private static string GetFilterText<T>(IEnumerable<EventFilterOption<T>> options, string placeholder)
    {
        var selected = options.Where(option => option.IsSelected).Select(option => option.DisplayText).ToArray();
        return selected.Length == 0 ? placeholder : string.Join(", ", selected);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadEntriesFromPreviousEventAsync()
    {
        var window = _windowFactory.CreateAndShowAndReturn<LoadEntriesFromPreviousEventWindow>();
        if (window.DataContext is not IWindowResult { Result: LoadEntriesFromPreviousEventResult selection })
            return;

        try
        {
            var (created, errors) = await entryService.CopyEntriesFromPreviousEventAsync(
                selection.PreviousEventId,
                selection.TargetEventId);

            ShowEventImportResult(selection, created.Count, errors);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ShowEventImportFailure(selection, ex.Message);
        }
    }

    private void ShowEventImportResult(
        LoadEntriesFromPreviousEventResult selection,
        int createdCount,
        IReadOnlyList<ValidationResult> errors)
    {
        ImportBarKind = EntriesImportBarKind.Event;
        EventImportPreviousEventName = selection.PreviousEventDisplayName;
        EventImportTargetEventName = selection.TargetEventDisplayName;
        EventImportCreatedCount = createdCount;
        EventImportErrors = new ObservableCollection<string>(
            errors.Select(e => e.ErrorMessage).OfType<string>().Where(message => message.Length > 0));

        IsImportRunning = false;
        IsImportDetailsOpen = EventImportErrors.Count > 0;

        if (errors.Count == 0)
        {
            ImportHeader = Strings.Import_Event_Success_Header;
            ImportMessage = string.Format(
                Strings.Import_Event_Success_MessageFormat,
                createdCount,
                selection.TargetEventDisplayName);
        }
        else if (createdCount > 0)
        {
            ImportHeader = Strings.Import_Event_Partial_Header;
            ImportMessage = string.Format(
                Strings.Import_Event_Partial_MessageFormat,
                createdCount,
                errors.Count,
                selection.TargetEventDisplayName);
        }
        else
        {
            ImportHeader = Strings.Import_Event_Failed_Header;
            ImportMessage = string.Format(Strings.Import_Event_Failed_MessageFormat, errors.Count);
        }

        IsImportBarOpen = true;
    }

    private void ShowEventImportFailure(LoadEntriesFromPreviousEventResult selection, string message)
    {
        ImportBarKind = EntriesImportBarKind.Event;
        EventImportPreviousEventName = selection.PreviousEventDisplayName;
        EventImportTargetEventName = selection.TargetEventDisplayName;
        EventImportCreatedCount = 0;
        EventImportErrors = new ObservableCollection<string> { message };
        IsImportRunning = false;
        IsImportDetailsOpen = true;
        ImportHeader = Strings.Import_Event_Error_Header;
        ImportMessage = message;
        IsImportBarOpen = true;
    }

    [RelayCommand]
    private async Task ImportEntriesFromFileAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = Strings.Dialog_SelectEntryFiles_Title,
            Filter = Strings.Dialog_SelectEntryFiles_Filter,
            Multiselect = true
        };

        if (openFileDialog.ShowDialog() != true)
            return;

        await StartImportAsync(openFileDialog.FileNames);
    }

    [RelayCommand(CanExecute = nameof(CanCancelImport))]
    private void CancelImport()
    {
        _importCts?.Cancel();
    }

    private bool CanCancelImport()
    {
        return IsImportRunning;
    }

    [RelayCommand]
    private void ToggleImportDetails()
    {
        IsImportDetailsOpen = !IsImportDetailsOpen;
    }

    [RelayCommand]
    private void DismissImportBar()
    {
        if (IsImportRunning) return;
        IsImportBarOpen = false;
        IsImportDetailsOpen = false;
        EventImportErrors.Clear();
        OnPropertyChanged(nameof(HasEventImportDetails));
    }

    partial void OnEventImportErrorsChanged(ObservableCollection<string> value) =>
        OnPropertyChanged(nameof(HasEventImportDetails));

    public void OnNavigatedTo(object? parameter)
    {
        ResetFilterOptions();
        EnsureFilterOptionsInitialized();
        _ = LoadDataAsync();
    }

    public void OnNavigatedFrom()
    {
        _suppressImportUiUpdates = true;
        if (IsImportRunning)
            _importCts?.Cancel();
    }

    private async Task StartImportAsync(string[] files)
    {
        if (files.Length == 0) return;

        _importCts?.Cancel();
        _importCts = new CancellationTokenSource();
        _suppressImportUiUpdates = false;

        ImportBarKind = EntriesImportBarKind.File;
        EventImportErrors.Clear();

        var filesToImport = files.Select(f => new EntriesFile(Path.GetFileName(f), f)).ToList();
        ImportFiles = new ObservableCollection<EntriesFile>(filesToImport);
        RecalculateImportSummary();

        ImportTotalFiles = files.Length;
        ImportProcessedFiles = 0;
        IsImportRunning = true;
        IsImportBarOpen = true;
        ImportHeader = Strings.Import_File_Header;
        ImportMessage = string.Format(Strings.Import_File_Preparing_MessageFormat, ImportTotalFiles);
        CancelImportCommand.NotifyCanExecuteChanged();

        var importCanceled = false;
        var token = _importCts.Token;

        try
        {
            await Task.Run(async () =>
            {
                foreach (var file in filesToImport)
                {
                    token.ThrowIfCancellationRequested();

                    if (!_suppressImportUiUpdates)
                    {
                        await DispatcherUiHelper.RunOnUiAsync(() =>
                        {
                            file.Status = ImportFileStatus.Processing;
                            ImportMessage = string.Format(
                                Strings.Import_File_Processing_MessageFormat,
                                file.FileName,
                                ImportProcessedFiles + 1,
                                ImportTotalFiles);
                        }).ConfigureAwait(false);
                    }

                    await using var batch = new EntryImportBatchSession();
                    try
                    {
                        var (documents, stats) = batch.ImportFile(file.FullPath, token);

                        if (!_suppressImportUiUpdates)
                        {
                            await DispatcherUiHelper.RunOnUiAsync(() =>
                            {
                                ApplyImportStats(file, documents, stats);
                                file.Status = ResolveImportFileStatus(documents);
                                RecalculateImportSummary();
                                ImportProcessedFiles++;
                            }).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (!_suppressImportUiUpdates)
                        {
                            await DispatcherUiHelper.RunOnUiAsync(() =>
                            {
                                file.Status = ImportFileStatus.Canceled;
                                MarkUnfinishedImportFilesCanceled(filesToImport);
                                ImportProcessedFiles++;
                            }).ConfigureAwait(false);
                        }

                        throw;
                    }
                    catch (Exception)
                    {
                        if (!_suppressImportUiUpdates)
                        {
                            await DispatcherUiHelper.RunOnUiAsync(() =>
                            {
                                file.Status = ImportFileStatus.Failed;
                                ImportProcessedFiles++;
                            }).ConfigureAwait(false);
                        }
                    }
                }
            }, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            importCanceled = true;
            ImportHeader = Strings.Import_File_Canceled_Header;
            ImportMessage = string.Format(
                Strings.Operation_Canceled_MessageFormat,
                ImportProcessedFiles,
                ImportTotalFiles);
        }
        finally
        {
            if (!_suppressImportUiUpdates)
            {
                if (!importCanceled)
                {
                    ImportMessage = string.Format(
                        Strings.Import_File_Finished_MessageFormat,
                        ImportProcessedFiles,
                        ImportTotalFiles);
                }

                IsImportRunning = false;
                CancelImportCommand.NotifyCanExecuteChanged();
                RecalculateImportSummary();
            }
        }

        if (!importCanceled && !_suppressImportUiUpdates)
            await LoadDataAsync();
    }

    private static void ApplyImportStats(
        EntriesFile file,
        IReadOnlyList<EntryDocument> documents,
        EntryImportStats stats)
    {
        file.ClubsScanned = stats.ClubsScanned;
        file.ClubsWithErrors = stats.ClubsWithErrors;
        file.ClubsAdded = stats.ClubsAdded;
        file.ClubsUpdated = stats.ClubsUpdated;
        file.AthletesScanned = stats.AthletesScanned;
        file.AthletesWithErrors = stats.AthletesWithErrors;
        file.AthletesAdded = stats.AthletesAdded;
        file.AthletesUpdated = stats.AthletesUpdated;
        file.EntriesScanned = stats.EntriesScanned;
        file.EntriesWithErrors = stats.EntriesWithErrors;
        file.EntriesAdded = stats.EntriesAdded;
        file.EntriesUpdated = stats.EntriesUpdated;
        file.Warnings = documents.SelectMany(d => d.Warnings).ToArray();
        file.Errors = documents.SelectMany(d => d.Errors).ToArray();
        file.WarningsCount = file.Warnings.Count;
        file.ErrorsCount = file.Errors.Count;
    }

    private static ImportFileStatus ResolveImportFileStatus(IReadOnlyList<EntryDocument> documents)
    {
        var hasErrors = documents.Any(d => d.Errors.Count > 0);
        var hasWarnings = documents.Any(d => d.Warnings.Count > 0);
        return hasErrors
            ? ImportFileStatus.Failed
            : hasWarnings
                ? ImportFileStatus.CompletedWithWarnings
                : ImportFileStatus.Completed;
    }

    private static void MarkUnfinishedImportFilesCanceled(IEnumerable<EntriesFile> files)
    {
        foreach (var file in files)
        {
            if (file.IsSummaryRow)
                continue;

            if (file.Status is ImportFileStatus.Pending or ImportFileStatus.Processing)
                file.Status = ImportFileStatus.Canceled;
        }
    }

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
}