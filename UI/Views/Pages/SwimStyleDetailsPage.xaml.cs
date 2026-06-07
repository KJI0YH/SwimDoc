using System.Windows.Controls;
using SwimStyleDetailsViewModel = UI.ViewModels.Pages.SwimStyleDetailsViewModel;

namespace UI.Views.Pages;

public partial class SwimStyleDetailsPage : Page
{
    public SwimStyleDetailsPage(SwimStyleDetailsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
