using System.Windows.Controls;
using UI.ViewModels.Table;

namespace UI.Views.Pages;

public partial class EntriesPage : Page
{
    public EntriesPage(EntriesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
