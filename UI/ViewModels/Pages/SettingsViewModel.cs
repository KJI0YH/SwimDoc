using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ServiceLayer.EntryDocumentTemplateService;
using UI.Resources;
using UI.Services;

namespace UI.ViewModels.Pages;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IEntryDocumentTemplateService _entryDocumentTemplateService;
    private readonly ILocalizationService _localizationService;

    public ObservableCollection<AppLanguage> AvailableLanguages { get; } =
        new([AppLanguage.Russian, AppLanguage.English]);

    public BaseTimesSettingsViewModel BaseTimes { get; }

    [ObservableProperty] private AppLanguage _selectedLanguage;
    [ObservableProperty] private bool _isBaseTimesOpen;

    public SettingsViewModel(
        IEntryDocumentTemplateService entryDocumentTemplateService,
        ILocalizationService localizationService,
        BaseTimesSettingsViewModel baseTimesSettingsViewModel)
    {
        _entryDocumentTemplateService = entryDocumentTemplateService;
        _localizationService = localizationService;
        BaseTimes = baseTimesSettingsViewModel;
        _selectedLanguage = localizationService.CurrentLanguage;
    }

    partial void OnSelectedLanguageChanged(AppLanguage value)
    {
        _localizationService.SetLanguage(value);
        BaseTimes.RefreshDisplayNames();
    }

    [RelayCommand]
    private void OpenBaseTimes()
    {
        BaseTimes.RefreshDisplayNames();
        IsBaseTimesOpen = true;
    }

    [RelayCommand]
    private void CloseBaseTimes()
    {
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
