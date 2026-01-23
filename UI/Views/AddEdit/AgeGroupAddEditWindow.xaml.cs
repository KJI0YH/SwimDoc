using System.Windows;
using DataLayer.EfClasses;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.Crud;
using UI.ViewModels.AddEdit;

namespace UI.Views.AddEdit;

public partial class AgeGroupAddEditWindow : Window
{
    public AgeGroupAddEditWindow(int? id)
    {
        InitializeComponent();
        var ageGroupService = App.Current.Services.GetRequiredService<IAgeGroupService>();
        var viewModel = new AgeGroupAddAddEditViewModel(id, ageGroupService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}
