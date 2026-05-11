using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using UI.ViewModels.Windows.AddEdit;

namespace UI.Views.Windows.AddEdit;

public partial class EntryAddEditWindow : Window
{
    private bool _initialized;

    public EntryAddEditWindow(int? id)
    {
        InitializeComponent();
        var entryService = App.Current.Services.GetRequiredService<IEntryService>();
        var athleteService = App.Current.Services.GetRequiredService<IAthleteService>();
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        var clubService = App.Current.Services.GetRequiredService<IClubService>();
        var viewModel = new EntryViewModel(id, entryService, athleteService, clubService, eventService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            if (_initialized) return;
            _initialized = true;
            if (!viewModel.IsInitialized)
                await viewModel.InitializeAsync();
        };
    }
}