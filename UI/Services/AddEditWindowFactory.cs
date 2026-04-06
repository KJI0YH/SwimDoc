using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;
using UI.Views;

namespace UI.Services;

public class AddEditWindowFactory : IAddEditWindowFactory
{
    private static readonly Dictionary<Window, (int Count, double OriginalOpacity)> DimStates = new();

    private static Window? GetActiveWindow()
    {
        return Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
    }

    private static void PushDim(Window owner)
    {
        if (DimStates.TryGetValue(owner, out var state))
        {
            DimStates[owner] = (state.Count + 1, state.OriginalOpacity);
            return;
        }

        DimStates[owner] = (1, owner.Opacity);
        owner.Opacity = 0.75;
    }

    private static void PopDim(Window owner)
    {
        if (!DimStates.TryGetValue(owner, out var state))
            return;

        if (state.Count <= 1)
        {
            owner.Opacity = state.OriginalOpacity;
            DimStates.Remove(owner);
            return;
        }

        DimStates[owner] = (state.Count - 1, state.OriginalOpacity);
    }

    public bool? CreateAndShow<TWindow>(int? id = null) where TWindow : Window
    {
        return CreateAndShow<TWindow>(id, null);
    }

    public bool? CreateAndShow<TWindow>(int? id, AddEditContext? context) where TWindow : Window
    {
        var windowType = typeof(TWindow);
        var nullableIntType = typeof(int?);
        
        // Ищем конструктор с параметром int?
        var constructor = windowType.GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { nullableIntType },
            null);
        
        if (constructor == null)
        {
            // Пробуем найти среди всех публичных конструкторов
            var constructors = windowType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            constructor = constructors.FirstOrDefault(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == nullableIntType;
            });
        }
        
        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"Window type {windowType.Name} does not have a constructor with parameter (int? id).");
        }
        
        // Создаем экземпляр окна
        var window = (TWindow)constructor.Invoke([id])
            ?? throw new InvalidOperationException($"Failed to create instance of {windowType.Name}.");
        ApplyContextIfNeeded(window, context);
        var owner = GetActiveWindow() ?? Application.Current.MainWindow;
        if (owner != null)
        {
            window.Owner = owner;
            if (owner is MainWindow mainWindow)
            {
                mainWindow.ShowModalOverlay();
                mainWindow.Dispatcher.Invoke(DispatcherPriority.Render, static () => { });
            }
            else
            {
                PushDim(owner);
                owner.Dispatcher.Invoke(DispatcherPriority.Render, static () => { });
            }
        }
        try
        {
            return window.ShowDialog();
        }
        finally
        {
            if (owner is MainWindow mw)
                mw.HideModalOverlay();
            else if (owner != null)
                PopDim(owner);
        }
    }

    public TWindow CreateAndShowAndReturn<TWindow>(int? id = null) where TWindow : Window
    {
        return CreateAndShowAndReturn<TWindow>(id, null);
    }

    public TWindow CreateAndShowAndReturn<TWindow>(int? id, AddEditContext? context) where TWindow : Window
    {
        var windowType = typeof(TWindow);
        var nullableIntType = typeof(int?);

        var constructor = windowType.GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            null,
            [nullableIntType],
            null);

        if (constructor == null)
        {
            var constructors = windowType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            constructor = constructors.FirstOrDefault(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == nullableIntType;
            });
        }

        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"Window type {windowType.Name} does not have a constructor with parameter (int? id).");
        }

        var window = (TWindow)constructor.Invoke([id])
            ?? throw new InvalidOperationException($"Failed to create instance of {windowType.Name}.");
        ApplyContextIfNeeded(window, context);
        var owner = GetActiveWindow() ?? Application.Current.MainWindow;
        if (owner != null)
        {
            window.Owner = owner;
            if (owner is MainWindow mainWindow)
            {
                mainWindow.ShowModalOverlay();
                mainWindow.Dispatcher.Invoke(DispatcherPriority.Render, static () => { });
            }
            else
            {
                PushDim(owner);
                owner.Dispatcher.Invoke(DispatcherPriority.Render, static () => { });
            }
        }
        try
        {
            _ = window.ShowDialog();
        }
        finally
        {
            if (owner is MainWindow mw)
                mw.HideModalOverlay();
            else if (owner != null)
                PopDim(owner);
        }
        return window;
    }

    private static void ApplyContextIfNeeded(Window window, AddEditContext? context)
    {
        var dataContext = window.DataContext;
        if (dataContext == null)
            return;

        if (context == null)
            return;

        if (dataContext is IAddEditContextAware aware)
            aware.ApplyContext(context);

        // Some add/edit windows initialize view models in constructor.
        // Re-run initialization after context application to apply filtered lists/defaults.
        var initializeAsyncMethod = dataContext.GetType().GetMethod("InitializeAsync", Type.EmptyTypes);
        _ = initializeAsyncMethod?.Invoke(dataContext, null);
    }
}
