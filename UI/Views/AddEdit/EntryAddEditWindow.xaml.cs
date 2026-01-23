using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.SwimStyleService;
using UI.ViewModels.AddEdit;

namespace UI.Views.AddEdit;

public partial class EntryAddEditWindow : Window
{
    public EntryAddEditWindow(int? id)
    {
        InitializeComponent();
        var entryService = App.Current.Services.GetRequiredService<IEntryService>();
        var athleteService = App.Current.Services.GetRequiredService<IAthleteService>();
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        var viewModel = new EntryAddEditViewModel(id, entryService, athleteService, eventService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}
