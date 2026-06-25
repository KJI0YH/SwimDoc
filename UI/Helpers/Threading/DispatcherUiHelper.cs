using System.Windows;
using System.Windows.Threading;

namespace UI.Helpers.Threading;

public static class DispatcherUiHelper
{
    public static async Task YieldForRenderAsync()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return;
        await dispatcher.InvokeAsync(static () => { }, DispatcherPriority.Render);
        await dispatcher.InvokeAsync(static () => { }, DispatcherPriority.Background);
    }

    public static async Task YieldToBackgroundAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public static Task InvokeOnUiAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action, DispatcherPriority.Normal).Task;
    }

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
