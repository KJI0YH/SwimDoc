using System.IO;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ServiceLayer.ConnectionService;
using ServiceLayer.Logging;
using UI.Resources;

namespace UI.ViewModels.Pages;

public partial class CompetitionSelectionViewModel(
    ICompetitionDatabaseService competitionDatabaseService,
    IAppLog log) : ViewModelBase
{
    public event Action<string>? CompetitionSelected;

    [RelayCommand]
    private async Task CreateNew()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = Strings.Dialog_CompetitionDb_Filter,
            Title = Strings.Dialog_CreateCompetition_Title,
            DefaultExt = Strings.Dialog_CompetitionDb_DefaultExt
        };
        if (saveFileDialog.ShowDialog() == true)
        {
            log.Info($"Create competition requested: {saveFileDialog.FileName}");
            try
            {
                File.Create(saveFileDialog.FileName).Close();
                await TryOpenCompetitionFileAsync(saveFileDialog.FileName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                log.Error($"Failed to create competition file: {saveFileDialog.FileName}", ex);
                var dialogs = App.Current.Services.GetRequiredService<IErrorDialogService>();
                await dialogs.ShowErrorAsync(
                    title: Strings.Dialog_Error_CreateDbFile_Title,
                    message: string.Format(Strings.Dialog_Error_FileBusyOrUnavailableWithDetailsFormat, ex.Message));
            }
        }
    }

    [RelayCommand]
    private async Task OpenExisting()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = Strings.Dialog_CompetitionDb_OpenFilter,
            Title = Strings.Dialog_OpenCompetition_Title
        };
        if (openFileDialog.ShowDialog() == true)
        {
            log.Info($"Open competition requested: {openFileDialog.FileName}");
            await TryOpenCompetitionFileAsync(openFileDialog.FileName);
        }
    }

    public async Task<bool> TryOpenCompetitionFileAsync(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            log.Warning($"Competition file not found: {fullPath}");
            await ShowOpenErrorAsync(string.Format(Strings.Dialog_Error_CompetitionFileNotFoundFormat, fullPath));
            return false;
        }

        log.Info($"Opening competition database: {fullPath}");
        var result = await competitionDatabaseService.TryOpenAsync(fullPath);
        if (!result.Success)
        {
            var details = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? Strings.Dialog_Error_OpenCompetition_NoDetails
                : result.ErrorMessage;
            log.Error($"Failed to open competition database: {fullPath}. {details}");
            await ShowOpenErrorAsync(string.Format(Strings.Dialog_Error_OpenCompetition_MessageFormat, details));
            return false;
        }

        log.Info($"Competition opened: {fullPath}");
        CompetitionSelected?.Invoke(fullPath);
        return true;
    }

    private static async Task ShowOpenErrorAsync(string message)
    {
        var dialogs = App.Current.Services.GetRequiredService<IErrorDialogService>();
        await dialogs.ShowErrorAsync(
            title: Strings.Dialog_OpenCompetition_Title,
            message: message);
    }
}
