using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using BizLogic.EntryDocumentReaderLogic;
using BizLogic.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.Views.Windows.AddEdit;
using QueryableSortByDirection = UI.ViewModels.Generic.QueryableSortByDirection;

namespace UI.ViewModels.Pages;

public partial class EntriesViewModel(
    IEntryService entryService,
    IEntryDocumentReaderService entryDocumentReaderService)
    : DataViewModel<Entry, int?>(entryService)
{
    public enum ImportFileStatus
    {
        [Description(" ")] Summary,
        [Description("В очереди")] Pending,
        [Description("В обработке")] Processing,
        [Description("Обработан")] Completed,

        [Description("Обработан с предупреждениями")]
        CompletedWithWarnings,
        [Description("Сбой")] Failed,
        [Description("Отменён")] Canceled
    }

    private readonly IAddEditWindowFactory _windowFactory =
        App.Current.Services.GetRequiredService<IAddEditWindowFactory>();

    private CancellationTokenSource? _importCts;
    [ObservableProperty] private ObservableCollection<EntriesFile> _importFiles = new();
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

    [ObservableProperty] private bool _isImportBarOpen;
    [ObservableProperty] private bool _isImportDetailsOpen;
    [ObservableProperty] private bool _isImportRunning;

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

        _summaryRow ??= new EntriesFile("Итого", string.Empty) { IsSummaryRow = true };
        if (!files.Contains(_summaryRow))
            files.Add(_summaryRow);

        _summaryRow.FileName = "Итого";
        _summaryRow.FullPath = $"Файлов: {ImportSummaryFilesCount}";
        _summaryRow.ClubsAdded = ImportSummaryClubsAdded;
        _summaryRow.ClubsUpdated = ImportSummaryClubsUpdated;
        _summaryRow.AthletesAdded = ImportSummaryAthletesAdded;
        _summaryRow.AthletesUpdated = ImportSummaryAthletesUpdated;
        _summaryRow.EntriesAdded = ImportSummaryEntriesAdded;
        _summaryRow.EntriesUpdated = ImportSummaryEntriesUpdated;
        _summaryRow.WarningsCount = ImportSummaryWarningsCount;
        _summaryRow.ErrorsCount = ImportSummaryErrorsCount;
        _summaryRow.Warnings = Array.Empty<string>();
        _summaryRow.Errors = Array.Empty<string>();
        _summaryRow.IsDetailsOpen = false;
        _summaryRow.Status = ImportFileStatus.Summary;
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("DisplaySwimName", "Дистанция", 500,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.SwimEvent.Order)
                    : query.OrderByDescending(e => e.SwimEvent.Order);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("Athlete.DisplayName", "Участник", 300,
            (query, direction) => QueryableSortByDirection.Sort(query, direction,
                q => Queryable
                    .OrderBy<Entry, string>(q, e => e.Athlete != null ? e.Athlete.LastName : null)
                    .ThenBy(e => e.Athlete != null ? e.Athlete.FirstName : null),
                q => Queryable
                    .OrderByDescending<Entry, string>(q, e => e.Athlete != null ? e.Athlete.LastName : null)
                    .ThenByDescending(e => e.Athlete != null ? e.Athlete.FirstName : null))));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("Status", "Статус", 150));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("DisplayEntryTime", "Заявочное время", 130,
            nameof(Entry.EntryTime)));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("DisplayFinishTime", "Финишное время", 130,
            nameof(Entry.FinishTime)));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("Points", "Очки", 100));
        ColumnConfigurations.Add(new ColumnConfiguration<Entry>("Comment", "Примечание", 100));
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        return query
            .Include(entry => entry.Athlete)
            .Include(entry => entry.SwimStyle)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(se => se.SwimStyle)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(se => se.AgeGroup)
            .Include(entry => entry.HeatPosition)
            .ThenInclude(heatPosition => heatPosition.Heat);
    }

    protected override IQueryable<Entry> ApplySearch(IQueryable<Entry> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;

        if (EnumHelper.TryGetEnumByDescriptionContains<EntryStatus>(SearchText, out var status))
            return query.Where(e => e.Status == status);

        if (EnumHelper.TryGetEnumByDescriptionContains<Gender>(SearchText, out var gender))
            return query.Where(e => e.SwimEvent.AgeGroup.Gender == gender);

        if (EnumHelper.TryGetEnumByDescriptionContains<Stroke>(SearchText, out var stroke))
            return query.Where(e => e.SwimStyle.Stroke == stroke);

        return Queryable.Where(query, e =>
            EF.Functions.Like(e.Athlete.FirstName, $"%{SearchText}%") ||
            EF.Functions.Like(e.Athlete.LastName, $"%{SearchText}%"));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task ImportEntriesFromFileAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Выберите файлы заявок",
            Filter = "Excel (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
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
    }

    private async Task StartImportAsync(string[] files)
    {
        if (files.Length == 0) return;

        _importCts?.Cancel();
        _importCts = new CancellationTokenSource();

        var filesToImport = files.Select(f => new EntriesFile(Path.GetFileName(f), f)).ToList();
        ImportFiles = new ObservableCollection<EntriesFile>(filesToImport);
        RecalculateImportSummary();

        ImportTotalFiles = files.Length;
        ImportProcessedFiles = 0;
        IsImportRunning = true;
        IsImportBarOpen = true;
        ImportHeader = "Импорт заявок";
        ImportMessage = $"Подготовка {ImportTotalFiles} файлов";
        CancelImportCommand.NotifyCanExecuteChanged();

        try
        {
            foreach (var file in filesToImport)
            {
                _importCts.Token.ThrowIfCancellationRequested();

                file.Status = ImportFileStatus.Processing;
                ImportMessage = $"Загрузка: {file.FileName} {ImportProcessedFiles + 1}/{ImportTotalFiles}";

                try
                {
                    var (documents, stats) = await Task.Run<(IReadOnlyList<EntryDocument> documents, EntryImportStats stats)>(
                        () => entryDocumentReaderService.ReadWithStats(file.FullPath),
                        _importCts.Token);

                    file.ClubsAdded = stats.ClubsAdded;
                    file.ClubsUpdated = stats.ClubsUpdated;
                    file.AthletesAdded = stats.AthletesAdded;
                    file.AthletesUpdated = stats.AthletesUpdated;
                    file.EntriesAdded = stats.EntriesAdded;
                    file.EntriesUpdated = stats.EntriesUpdated;
                    file.Warnings = documents.SelectMany(d => d.Warnings).ToArray();
                    file.Errors = documents.SelectMany(d => d.Errors).ToArray();
                    file.WarningsCount = file.Warnings.Count;
                    file.ErrorsCount = file.Errors.Count;

                    var hasErrors = documents.Any(d => (d.Errors?.Count ?? 0) > 0);
                    var hasWarnings = documents.Any(d => (d.Warnings?.Count ?? 0) > 0);
                    file.Status = hasErrors
                        ? ImportFileStatus.Failed
                        : hasWarnings
                            ? ImportFileStatus.CompletedWithWarnings
                            : ImportFileStatus.Completed;

                    RecalculateImportSummary();
                }
                catch (OperationCanceledException)
                {
                    file.Status = ImportFileStatus.Canceled;
                    throw;
                }
                catch (Exception ex)
                {
                    file.Status = ImportFileStatus.Failed;
                }
                finally
                {
                    ImportProcessedFiles++;
                }
            }
        }
        catch (OperationCanceledException)
        {
            ImportHeader = "Загрузка отменена";
            return;
        }
        finally
        {
            ImportMessage = $"Загружено {ImportProcessedFiles}/{ImportTotalFiles} файлов";
            IsImportRunning = false;
            CancelImportCommand.NotifyCanExecuteChanged();
            RecalculateImportSummary();
        }

        await LoadDataAsync();
    }

    public partial class EntriesFile : ObservableObject
    {
        [ObservableProperty] private int _athletesAdded;
        [ObservableProperty] private int _athletesUpdated;

        [ObservableProperty] private int _clubsAdded;
        [ObservableProperty] private int _clubsUpdated;
        [ObservableProperty] private int _entriesAdded;
        [ObservableProperty] private int _entriesUpdated;
        [ObservableProperty] private IReadOnlyList<string> _errors = Array.Empty<string>();
        [ObservableProperty] private int _errorsCount;

        [ObservableProperty] private string _fileName;
        [ObservableProperty] private string _fullPath;

        [ObservableProperty] private bool _isDetailsOpen;
        [ObservableProperty] private bool _isSummaryRow;

        [ObservableProperty] private ImportFileStatus _status = ImportFileStatus.Pending;
        [ObservableProperty] private IReadOnlyList<string> _warnings = Array.Empty<string>();
        [ObservableProperty] private int _warningsCount;

        public EntriesFile(string fileName, string fullPath)
        {
            FileName = fileName;
            FullPath = fullPath;
        }
    }
}