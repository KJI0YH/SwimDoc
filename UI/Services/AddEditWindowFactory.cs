using System.Windows;
using System.Windows.Threading;
using UI.Resources;
using UI.ViewModels.Dialogs.AddEdit;
using UI.Views.Controls.AddEditView;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace UI.Services;

public class AddEditWindowFactory(IContentDialogService contentDialogService) : IAddEditWindowFactory
{
    private readonly AddEditDialogRegistry _registry = new();
    private readonly AddEditDialogStack _dialogStack = new(contentDialogService);

    public bool? CreateAndShow<TDialog>(int? id = null, NavigationContext? context = null) where TDialog : class
    {
        var result = CreateAndShowAndReturn<TDialog>(id, context);
        return result.DialogResult;
    }

    public AddEditDialogResult CreateAndShowAndReturn<TDialog>(int? id = null, NavigationContext? context = null)
        where TDialog : class
    {
        return RunSync(() => ShowRegisteredDialogAsync(typeof(TDialog), id, context));
    }

    public bool? ShowGenericAddEdit(object? id, object crudService)
    {
        return RunSync(() => ShowGenericDialogAsync(id, crudService)).DialogResult;
    }

    private async Task<AddEditDialogResult> ShowRegisteredDialogAsync(Type dialogType, int? id, NavigationContext? context)
    {
        if (!_registry.TryGet(dialogType, out var definition))
            throw new InvalidOperationException($"Dialog type {dialogType.Name} is not registered.");

        var viewModel = definition.CreateViewModel(id);
        ApplyContextIfNeeded(viewModel, NavigationContext.Merge(id, context));
        await InitializeIfNeededAsync(viewModel);

        if (viewModel is EntryViewModel entryViewModel && !entryViewModel.IsInitialized)
            await entryViewModel.InitializeAsync();

        var view = definition.CreateView();
        view.DataContext = viewModel;

        return await ShowDialogAsync(viewModel, view);
    }

    private async Task<AddEditDialogResult> ShowGenericDialogAsync(object? id, object crudService)
    {
        var viewModel = _registry.CreateGenericViewModel(id, crudService);
        await InitializeIfNeededAsync(viewModel);

        var view = _registry.CreateGenericView();
        view.DataContext = viewModel;

        return await ShowDialogAsync(viewModel, view);
    }

    private async Task<AddEditDialogResult> ShowDialogAsync(object viewModel, FrameworkElement view)
    {
        var title = viewModel.GetType().GetProperty("WindowTitle")?.GetValue(viewModel) as string
                    ?? Strings.WindowMode_Edit;

        var content = new AddEditDialogContent
        {
            DataContext = viewModel,
            EditorContent = view
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = Strings.Common_Save,
            CloseButtonText = Strings.Common_Cancel,
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await _dialogStack.ShowAsync(dialog, viewModel);
        return new AddEditDialogResult(ToDialogResult(result), viewModel);
    }

    private static bool? ToDialogResult(ContentDialogResult result) =>
        result switch
        {
            ContentDialogResult.Primary => true,
            ContentDialogResult.None => false,
            _ => null
        };

    private static async Task InitializeIfNeededAsync(object viewModel)
    {
        var initializeAsyncMethod = viewModel.GetType().GetMethod("InitializeAsync", Type.EmptyTypes);
        if (initializeAsyncMethod == null)
            return;

        var task = initializeAsyncMethod.Invoke(viewModel, null) as Task;
        if (task != null)
            await task;
    }

    private static void ApplyContextIfNeeded(object viewModel, NavigationContext? context)
    {
        if (context is null)
            return;

        if (viewModel is INavigationContextAware aware)
            aware.ApplyContext(context);
    }

    private static T RunSync<T>(Func<Task<T>> asyncFunc)
    {
        var task = asyncFunc();
        if (task.IsCompletedSuccessfully)
            return task.Result;

        var frame = new DispatcherFrame();
        task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        return task.GetAwaiter().GetResult();
    }
}
