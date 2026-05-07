using System.Windows.Controls;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

