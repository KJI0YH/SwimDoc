using System.IO;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ServiceLayer.ConnectionService;
using UI.Resources;

namespace UI.ViewModels.Pages;

public partial class CompetitionSelectionViewModel(ICompetitionDatabaseService competitionDatabaseService) : ViewModelBase
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
            try
            {
                File.Create(saveFileDialog.FileName).Close();
                await TryOpenCompetitionFileAsync(saveFileDialog.FileName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
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
            await TryOpenCompetitionFileAsync(openFileDialog.FileName);
    }

    public async Task<bool> TryOpenCompetitionFileAsync(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            await ShowOpenErrorAsync(string.Format(Strings.Dialog_Error_CompetitionFileNotFoundFormat, fullPath));
            return false;
        }

        var result = await competitionDatabaseService.TryOpenAsync(fullPath);
        if (!result.Success)
        {
            var details = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? Strings.Dialog_Error_OpenCompetition_NoDetails
                : result.ErrorMessage;
            await ShowOpenErrorAsync(string.Format(Strings.Dialog_Error_OpenCompetition_MessageFormat, details));
            return false;
        }

        CompetitionSelected?.Invoke(fullPath);
        return true;
    }

    private static async Task ShowOpenErrorAsync(string message)
    {
        var dialogs = App.Current.Services.GetRequiredService<IErrorDialogService>();
        await dialogs.ShowErrorAsync(
            title: Strings.Dialog_Error_OpenCompetition_Title,
            message: message);
    }
}
