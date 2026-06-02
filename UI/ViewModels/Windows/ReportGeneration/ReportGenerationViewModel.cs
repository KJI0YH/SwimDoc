using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UI.Resources;
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
    [ObservableProperty] private bool _includeEntryList = false;
    [ObservableProperty] private bool _includeStartList = false;
    [ObservableProperty] private bool _includeFinishList = false;
    [ObservableProperty] private string _outputFilePath = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _validationErrors = [];

    public ReportGenerationViewModel()
    {
        ValidationErrors.CollectionChanged += OnValidationErrorsChanged;
    }

    public string WindowTitle => Strings.Reports_WindowTitle;

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
            Title = Strings.Reports_SaveDialog_Title,
            Filter = Strings.Dialog_SaveExcelReports_Filter,
            DefaultExt = Strings.Dialog_SaveExcelReports_DefaultExt,
            AddExtension = true,
            FileName = Strings.Dialog_SaveExcelReports_DefaultFileName
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
            ValidationErrors.Add(Strings.Reports_Validation_SelectAtLeastOne);
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputFilePath))
        {
            ValidationErrors.Add(Strings.Reports_Validation_SelectOutputFile);
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

