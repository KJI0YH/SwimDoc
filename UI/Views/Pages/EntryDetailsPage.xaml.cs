using System.Windows.Controls;
using EntryDetailsViewModel = UI.ViewModels.Pages.EntryDetailsViewModel;

namespace UI.Views.Pages;

public partial class EntryDetailsPage : Page
{
    public EntryDetailsPage(EntryDetailsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
