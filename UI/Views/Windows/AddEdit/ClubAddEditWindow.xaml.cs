using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.ClubService;
using UI.ViewModels.Windows.AddEdit;

namespace UI.Views.Windows.AddEdit;

public partial class ClubAddEditWindow : Window
{
    public ClubAddEditWindow(int? id)
    {
        InitializeComponent();
        var clubService = App.Current.Services.GetRequiredService<IClubService>();
        var viewModel = new ClubAddViewModel(id, clubService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}