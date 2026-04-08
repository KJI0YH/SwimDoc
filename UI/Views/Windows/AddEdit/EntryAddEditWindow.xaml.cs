using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using UI.ViewModels.Windows.AddEdit;

namespace UI.Views.Windows.AddEdit;

public partial class EntryAddEditWindow : Window
{
    public EntryAddEditWindow(int? id)
    {
        InitializeComponent();
        var entryService = App.Current.Services.GetRequiredService<IEntryService>();
        var athleteService = App.Current.Services.GetRequiredService<IAthleteService>();
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        var viewModel = new EntryViewModel(id, entryService, athleteService, eventService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}