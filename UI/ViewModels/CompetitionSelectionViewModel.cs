using System.IO;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace UI.ViewModels;

public partial class CompetitionSelectionViewModel : ViewModelBase
{
    public event Action<string>? CompetitionSelected;

    [RelayCommand]
    private void CreateNew()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "SQLite Database (*.db)|*.db|SQLite Database (*.sqlite)|*.sqlite|All Files (*.*)|*.*",
            Title = "Создать новое соревнование",
            DefaultExt = "db"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            InitializeDatabase(saveFileDialog.FileName);
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

        if (openFileDialog.ShowDialog() == true)
        {
            InitializeDatabase(openFileDialog.FileName);
        }
    }

    private void InitializeDatabase(string filePath)
    {
        var connectionString = $"Data Source={filePath}";
        App.Current.SetConnectionString(connectionString);
        CompetitionSelected?.Invoke(filePath);
    }
}