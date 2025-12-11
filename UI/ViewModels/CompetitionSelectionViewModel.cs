using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using UI.Services;

namespace UI.ViewModels;

public class CompetitionSelectionViewModel : ViewModelBase
{
    public CompetitionSelectionViewModel()
    {
        CreateNewCommand = new RelayCommand(CreateNew);
        OpenExistingCommand = new RelayCommand(OpenExisting);
    }

    public event Action<string>? CompetitionSelected;

    public ICommand CreateNewCommand { get; }
    public ICommand OpenExistingCommand { get; }

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