using System.Windows.Controls;
using UI.Helpers.Navigation;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class AthletesPage : Page
{
    public AthletesPage(AthletesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        DataPageNavigation.WireLoaded(this);
    }
}
