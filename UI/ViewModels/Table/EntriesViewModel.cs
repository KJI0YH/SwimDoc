using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BizLogic.EntryDocumentReaderLogic;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;

namespace UI.ViewModels.Table;

public partial class EntriesViewModel(
    IEntryService entryService,
    IEntryDocumentReaderService entryDocumentReaderService)
    : GenericTableViewModel<Entry, int?>(entryService)
{
    private readonly IAddEditWindowFactory _windowFactory =
        App.Current.Services.GetRequiredService<IAddEditWindowFactory>();

    private CancellationTokenSource? _importCts;

    [ObservableProperty] private bool _isImportBarOpen;
    [ObservableProperty] private bool _isImportDetailsOpen;
    [ObservableProperty] private bool _isImportRunning;
    [ObservableProperty] private int _importProcessedFiles;
    [ObservableProperty] private int _importTotalFiles;
    [ObservableProperty] private string _importHeader = string.Empty;
    [ObservableProperty] private string _importMessage = string.Empty;
    [ObservableProperty] private ObservableCollection<EntriesFile> _importFiles = new();

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplaySwimName", "Дистанция", 500));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Athlete.DisplayName", "Участник", 300));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Status", "Статус", 100));
        ColumnConfigurations.Add(new ColumnConfiguration
        {
            PropertyPath = "Scoring",
            Header = "В зачёт",
            Width = 60,
            TrueSymbolIcon = "Checkmark24",
            FalseSymbolIcon = "Dismiss24"
        });
        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayEntryTime", "Заявочное время", 130));
        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayFinishTime", "Финишное время", 130));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Points", "Очки", 100));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Comment", "Примечание", 100));
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        return query.Include(entry => entry.Athlete)
            .Include(entry => entry.SwimStyle)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(se => se.SwimStyle)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(se => se.AgeGroup)
            .Include(entry => entry.HeatPosition)
            .ThenInclude(heatPosition => heatPosition.Heat);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id);
        if (result == true)
        {
            _ = LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task ImportEntriesFromFileAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Выберите файлы заявок",
            Filter = "Excel (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            Multiselect = true,
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

    private bool CanCancelImport() => IsImportRunning;

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

        ImportFiles = new ObservableCollection<EntriesFile>(
            files.Select(f => new EntriesFile(Path.GetFileName(f), f)));

        ImportTotalFiles = files.Length;
        ImportProcessedFiles = 0;
        IsImportRunning = true;
        IsImportBarOpen = true;
        ImportHeader = "Импорт заявок";
        ImportMessage = $"Подготовка {ImportTotalFiles} файлов";
        CancelImportCommand.NotifyCanExecuteChanged();

        try
        {
            foreach (var file in ImportFiles)
            {
                _importCts.Token.ThrowIfCancellationRequested();

                file.Status = ImportFileStatus.Processing;
                ImportMessage = $"Загрузка: {file.FileName} {ImportProcessedFiles + 1}/{ImportTotalFiles}";

                try
                {
                    var (documents, stats) = await Task.Run(
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
        }

        await LoadDataAsync();
    }

    public partial class EntriesFile : ObservableObject
    {
        public EntriesFile(string fileName, string fullPath)
        {
            FileName = fileName;
            FullPath = fullPath;
        }

        public string FileName { get; }
        public string FullPath { get; }

        public IReadOnlyList<EntryDocument> EntryDocuments { get; set; }

        [ObservableProperty] private int _clubsAdded;
        [ObservableProperty] private int _clubsUpdated;
        [ObservableProperty] private int _athletesAdded;
        [ObservableProperty] private int _athletesUpdated;
        [ObservableProperty] private int _entriesAdded;
        [ObservableProperty] private int _entriesUpdated;
        [ObservableProperty] private int _warningsCount;
        [ObservableProperty] private int _errorsCount;
        [ObservableProperty] private IReadOnlyList<string> _warnings = Array.Empty<string>();
        [ObservableProperty] private IReadOnlyList<string> _errors = Array.Empty<string>();
        
        [ObservableProperty] private ImportFileStatus _status = ImportFileStatus.Pending;
    }

    public enum ImportFileStatus
    {
        [Description("В очереди")]Pending,
        [Description("В обработке")]Processing,
        [Description("Обработан")]Completed,
        [Description("Обработан с предупреждениями")]CompletedWithWarnings,
        [Description("Сбой")]Failed,
        [Description("Отменён")]Canceled
    }
}