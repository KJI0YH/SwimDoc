using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UI.Services;

namespace UI.ViewModels.Windows.ReportGeneration;

public sealed class ReportGenerationResult(bool entry, bool start, bool finish, string outputFilePath)
{
    public bool IncludeEntryList { get; } = entry;
    public bool IncludeStartList { get; } = start;
    public bool IncludeFinishList { get; } = finish;
    public string OutputFilePath { get; } = outputFilePath;
}

public partial class ReportGenerationViewModel : ViewModelBase, IWindowResult
{
    [ObservableProperty] private bool _includeEntryList = true;
    [ObservableProperty] private bool _includeStartList = true;
    [ObservableProperty] private bool _includeFinishList = true;
    [ObservableProperty] private string _outputFilePath = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _validationErrors = [];

    public ReportGenerationViewModel()
    {
        ValidationErrors.CollectionChanged += OnValidationErrorsChanged;
    }

    public string WindowTitle => "Генерация отчётов";

    public bool HasErrors => ValidationErrors.Count > 0;

    public ReportGenerationResult? Result { get; private set; }

    object? IWindowResult.Result => Result;

    public event EventHandler? CloseRequested;

    private void OnValidationErrorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasErrors));
    }

    [RelayCommand]
    private void Browse()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить отчёты",
            Filter = "Excel (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
            DefaultExt = "xlsx",
            AddExtension = true,
            FileName = "Reports.xlsx"
        };

        if (dialog.ShowDialog() == true)
            OutputFilePath = dialog.FileName;
    }

    [RelayCommand]
    private void Save()
    {
        ValidationErrors.Clear();

        if (!IncludeEntryList && !IncludeStartList && !IncludeFinishList)
        {
            ValidationErrors.Add("Выберите хотя бы один тип отчёта.");
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputFilePath))
        {
            ValidationErrors.Add("Выберите файл для сохранения (.xlsx).");
            return;
        }

        Result = new ReportGenerationResult(IncludeEntryList, IncludeStartList, IncludeFinishList, OutputFilePath);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

