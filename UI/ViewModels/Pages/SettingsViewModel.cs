using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using BizLogic.EntryDocumentReader;
using ServiceLayer.EntryDocumentTemplateService;
using ServiceLayer.EntryImportSettings;
using UI.Resources;
using UI.ViewModels;

namespace UI.ViewModels.Pages;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IEntryDocumentTemplateService _entryDocumentTemplateService;
    private readonly ILocalizationService _localizationService;
    private readonly IEntryImportSettingsService _entryImportSettingsService;
    public ObservableCollection<AppLanguage> AvailableLanguages { get; } =
        new([AppLanguage.Russian, AppLanguage.English]);

    public ObservableCollection<PagingSettingItemViewModel> PagingSettings { get; }
    public BaseTimesSettingsViewModel BaseTimes { get; }

    [ObservableProperty] private AppLanguage _selectedLanguage;
    [ObservableProperty] private bool _isBaseTimesOpen;
    [ObservableProperty] private EntryImportHighlightScoringMode _highlightScoringMode;
    public Array HighlightScoringModes => Enum.GetValues<EntryImportHighlightScoringMode>();
    public SettingsViewModel(
        IEntryDocumentTemplateService entryDocumentTemplateService,
        ILocalizationService localizationService,
        IEntryImportSettingsService entryImportSettingsService,
        IPagingSettingsService pagingSettingsService,
        BaseTimesSettingsViewModel baseTimesSettingsViewModel)
    {
        _entryDocumentTemplateService = entryDocumentTemplateService;
        _localizationService = localizationService;
        _entryImportSettingsService = entryImportSettingsService;
        BaseTimes = baseTimesSettingsViewModel;
        _selectedLanguage = localizationService.CurrentLanguage;
        _highlightScoringMode = entryImportSettingsService.HighlightScoringMode;
        PagingSettings = new ObservableCollection<PagingSettingItemViewModel>(
            PagingSettingsService.NavigationOrder
                .Select(page => new PagingSettingItemViewModel(pagingSettingsService, page)));
        _localizationService.CultureChanged += OnCultureChanged;
    }

    partial void OnSelectedLanguageChanged(AppLanguage value) =>
        _localizationService.SetLanguage(value);

    partial void OnHighlightScoringModeChanged(EntryImportHighlightScoringMode value) =>
        _entryImportSettingsService.SetHighlightScoringMode(value);

    private void OnCultureChanged(CultureInfo _)
    {
        foreach (var item in PagingSettings)
            item.RefreshDisplayText();
        BaseTimes.RefreshDisplayNames();
        OnPropertyChanged(nameof(HighlightScoringModes));
    }

    [RelayCommand]
    private void OpenBaseTimes()
    {
        BaseTimes.ReloadFromRepository();
        IsBaseTimesOpen = true;
    }

    [RelayCommand]
    private void CloseBaseTimes()
    {
        BaseTimes.ReloadFromRepository();
        IsBaseTimesOpen = false;
    }

    [RelayCommand]
    private async Task DownloadEntryDocumentTemplate()
    {
        var dialog = new SaveFileDialog
        {
            Title = Strings.Dialog_SaveExcelTemplate_Title,
            Filter = Strings.Dialog_SaveExcelTemplate_Filter,
            FileName = Strings.Dialog_SaveExcelTemplate_DefaultFileName,
            AddExtension = true,
            DefaultExt = ".xlsx",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true)
            return;
        try
        {
            var bytes = _entryDocumentTemplateService.CreateTemplate();
            File.WriteAllBytes(dialog.FileName, bytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var dialogs = App.Current.Services.GetRequiredService<IErrorDialogService>();
            await dialogs.ShowErrorAsync(
                title: Strings.Dialog_Error_SaveFile_Title,
                message: Strings.Dialog_Error_FileBusyOrUnavailable);
        }
    }
}
