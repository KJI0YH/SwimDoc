using System.Windows.Controls;
using EntriesViewModel = UI.ViewModels.Pages.EntriesViewModel;

namespace UI.Views.Pages;

public partial class EntriesPage : Page
{
    public EntriesPage(EntriesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
