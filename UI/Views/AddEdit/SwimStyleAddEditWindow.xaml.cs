using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Crud;
using ServiceLayer.SwimStyleService;
using UI.ViewModels.AddEdit;

namespace UI.Views.AddEdit;

public partial class SwimStyleAddEditWindow : Window
{
    public SwimStyleAddEditWindow(int? id)
    {
        InitializeComponent();
        var swimStyleService = App.Current.Services.GetRequiredService<ISwimStyleService>();
        var viewModel = new SwimStyleAddAddEditViewModel(id, swimStyleService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}
