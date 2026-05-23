using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EventService;
using UI.ViewModels.Windows.StartTimeCalculation;

namespace UI.Views.Windows.StartTimeCalculation;

public partial class StartTimeCalculationWindow : Window
{
    public StartTimeCalculationWindow(int? _ = null)
    {
        InitializeComponent();
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        var viewModel = new StartTimeCalculationViewModel(eventService);
        viewModel.CloseRequested += (_, _) =>
        {
            DialogResult = viewModel.Result is not null;
            Close();
        };

        DataContext = viewModel;
    }
}
