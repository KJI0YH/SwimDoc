using System.Windows.Controls;
using UI.Helpers.Navigation;
using HeatsViewModel = UI.ViewModels.Pages.HeatsViewModel;

namespace UI.Views.Pages;

public partial class HeatsPage : Page
{
    public HeatsPage(HeatsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        DataPageNavigation.WireLoaded(this);
    }
}
