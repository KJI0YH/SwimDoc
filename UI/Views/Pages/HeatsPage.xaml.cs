using System.Windows.Controls;
using UI.ViewModels.Table;

namespace UI.Views.Pages;

public partial class HeatsPage : Page
{
    public HeatsPage(HeatsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
