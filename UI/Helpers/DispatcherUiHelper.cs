using System.Windows;
using System.Windows.Threading;

namespace UI.Helpers;

public static class DispatcherUiHelper
{
    public static Task RunOnUiAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        return dispatcher is null || dispatcher.CheckAccess()
            ? RunAsync(action)
            : dispatcher.InvokeAsync(action, DispatcherPriority.Background).Task;

        static Task RunAsync(Action a)
        {
            a();
            return Task.CompletedTask;
        }
    }
}
