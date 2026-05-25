using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.ViewModels.Windows.AddEdit;

namespace UI.Views.Windows.AddEdit;

public partial class HeatAddEditWindow : Window
{
    public HeatAddEditWindow(int? id)
    {
        InitializeComponent();
        var heatService = App.Current.Services.GetRequiredService<IHeatService>();
        var entryService = App.Current.Services.GetRequiredService<IEntryService>();
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        var viewModel = new HeatAddEditViewModel(id, heatService, entryService, eventService);
        viewModel.CloseRequested += (_, _) =>
        {
            if (viewModel.WasSaved)
                DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}
