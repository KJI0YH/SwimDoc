using System.IO;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using UI.Services;
using UI.Resources;

namespace UI.ViewModels.Pages;

public partial class CompetitionSelectionViewModel : ViewModelBase
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
                InitializeDatabase(saveFileDialog.FileName);
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
    private void OpenExisting()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = Strings.Dialog_CompetitionDb_Filter,
            Title = Strings.Dialog_OpenCompetition_Title
        };

        if (openFileDialog.ShowDialog() == true) InitializeDatabase(openFileDialog.FileName);
    }

    private void InitializeDatabase(string filePath)
    {
        var connectionString = $"Data Source={filePath}";
        App.Current.SetConnectionString(connectionString);
        CompetitionSelected?.Invoke(filePath);
    }
}
