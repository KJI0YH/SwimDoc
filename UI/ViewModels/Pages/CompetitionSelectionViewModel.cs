using System.IO;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using UI.Services;

namespace UI.ViewModels.Pages;

public partial class CompetitionSelectionViewModel : ViewModelBase
{
    public event Action<string>? CompetitionSelected;

    [RelayCommand]
    private async Task CreateNew()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "SQLite Database (*.db)|*.db|SQLite Database (*.sqlite)|*.sqlite|All Files (*.*)|*.*",
            Title = "Создать новое соревнование",
            DefaultExt = "db"
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
                    title: "Не удалось создать файл базы данных",
                    message: $"Файл занят другим процессом или недоступен.\n\n{ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void OpenExisting()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "SQLite Database (*.db)|*.db|SQLite Database (*.sqlite)|*.sqlite|All Files (*.*)|*.*",
            Title = "Выберите файл соревнования"
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