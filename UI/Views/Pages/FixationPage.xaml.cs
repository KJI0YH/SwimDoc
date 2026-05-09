using System.Windows.Controls;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class FixationPage : Page
{
    public FixationPage(FixationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

