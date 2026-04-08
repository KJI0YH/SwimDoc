using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.SwimStyleService;
using UI.ViewModels.Windows.AddEdit;

namespace UI.Views.Windows.AddEdit;

public partial class SwimStyleAddEditWindow : Window
{
    public SwimStyleAddEditWindow(int? id)
    {
        InitializeComponent();
        var swimStyleService = App.Current.Services.GetRequiredService<ISwimStyleService>();
        var viewModel = new SwimStyleAddViewModel(id, swimStyleService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}