using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.Crud;
using UI.ViewModels.AddEdit;

namespace UI.Views.AddEdit;

public partial class ClubAddEditWindow : Window
{
    public ClubAddEditWindow(int? id)
    {
        InitializeComponent();
        var clubService = App.Current.Services.GetRequiredService<IClubService>();
        var viewModel = new ClubAddAddEditViewModel(id, clubService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}
