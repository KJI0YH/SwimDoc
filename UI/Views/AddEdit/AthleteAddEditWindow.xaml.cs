using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using UI.ViewModels.AddEdit;

namespace UI.Views.AddEdit;

public partial class AthleteAddEditWindow : Window
{
    public AthleteAddEditWindow(int? id)
    {
        InitializeComponent();
        var athleteService = App.Current.Services.GetRequiredService<IAthleteService>();
        var clubService = App.Current.Services.GetRequiredService<IClubService>();
        var viewModel = new AthleteAddAddEditViewModel(id, athleteService, clubService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}
