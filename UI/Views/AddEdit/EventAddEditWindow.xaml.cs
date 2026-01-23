using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EventService;
using ServiceLayer.SwimStyleService;
using UI.ViewModels.AddEdit;

namespace UI.Views.AddEdit;

public partial class EventAddEditWindow : Window
{
    public EventAddEditWindow(int? id)
    {
        InitializeComponent();
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        var ageGroupService = App.Current.Services.GetRequiredService<IAgeGroupService>();
        var swimStyleService = App.Current.Services.GetRequiredService<ISwimStyleService>();
        var viewModel = new EventAddAddEditViewModel(id, eventService, ageGroupService, swimStyleService);
        viewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = viewModel;
        _ = viewModel.InitializeAsync();
    }
}