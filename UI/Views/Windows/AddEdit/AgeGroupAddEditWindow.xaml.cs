using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using UI.ViewModels.Windows.AddEdit;

namespace UI.Views.Windows.AddEdit;

public partial class AgeGroupAddEditWindow : Window
{
    public AgeGroupAddEditWindow(int? id)
    {
        InitializeComponent();
        var ageGroupService = App.Current.Services.GetRequiredService<IAgeGroupService>();
        var viewModel = new AgeGroupAddViewModel(id, ageGroupService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}