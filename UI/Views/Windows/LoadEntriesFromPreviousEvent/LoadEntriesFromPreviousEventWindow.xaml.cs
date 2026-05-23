using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EventService;
using UI.ViewModels.Windows.LoadEntriesFromPreviousEvent;

namespace UI.Views.Windows.LoadEntriesFromPreviousEvent;

public partial class LoadEntriesFromPreviousEventWindow : Window
{
    public LoadEntriesFromPreviousEventWindow(int? _ = null)
    {
        InitializeComponent();
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        var viewModel = new LoadEntriesFromPreviousEventViewModel(eventService);
        viewModel.CloseRequested += (_, _) =>
        {
            DialogResult = viewModel.Result is not null;
            Close();
        };

        DataContext = viewModel;
    }
}
