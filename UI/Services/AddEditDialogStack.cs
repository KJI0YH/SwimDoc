using System.Windows;
using System.Windows.Input;
using UI.Views.Controls.AddEditView;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace UI.Services;

/// <summary>
/// Manages a stack of modal add/edit dialogs inside a single <see cref="ContentDialogHost"/>.
/// Supports unlimited nesting: closing a child dialog reveals the parent without completing it.
/// </summary>
internal sealed class AddEditDialogStack(IContentDialogService contentDialogService)
{
    private readonly Stack<DialogSession> _sessions = new();
    private DialogStackHost? _stackHost;

    public async Task<ContentDialogResult> ShowAsync(ContentDialog dialog, object viewModel)
    {
        var host = contentDialogService.GetDialogHostEx()
                   ?? throw new InvalidOperationException("ContentDialogHost is not configured.");

        EnsureStackHost(host);

        var session = new DialogSession(
            dialog,
            new TaskCompletionSource<ContentDialogResult>(TaskCreationOptions.RunContinuationsAsynchronously));

        WireDialog(dialog, viewModel, session);

        _sessions.Push(session);
        _stackHost!.Push(dialog);

        try
        {
            return await session.Completion.Task;
        }
        finally
        {
            if (_sessions.Count > 0 && ReferenceEquals(_sessions.Peek(), session))
                _sessions.Pop();

            _stackHost.Remove(dialog);

            if (_sessions.Count == 0)
            {
                host.Content = null;
                _stackHost = null;
            }
        }
    }

    private void EnsureStackHost(ContentDialogHost host)
    {
        if (_stackHost != null)
            return;

        if (host.Content is DialogStackHost existing)
        {
            _stackHost = existing;
            return;
        }

        if (host.Content != null)
            throw new InvalidOperationException("ContentDialogHost is already used by another dialog.");

        _stackHost = new DialogStackHost
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        host.Content = _stackHost;
    }

    private void WireDialog(ContentDialog dialog, object viewModel, DialogSession session)
    {
        dialog.DialogHostEx = contentDialogService.GetDialogHostEx();
        dialog.HorizontalAlignment = HorizontalAlignment.Stretch;
        dialog.VerticalAlignment = VerticalAlignment.Stretch;

        dialog.Closing += (_, e) =>
        {
            if (e.Result == ContentDialogResult.Primary && !session.CloseApproved)
                e.Cancel = true;
        };

        dialog.ButtonClicked += (_, e) =>
        {
            if (e.Button == ContentDialogButton.Primary)
                InvokeCommand(viewModel, "SaveCommand");
            else if (e.Button == ContentDialogButton.Close)
                InvokeCommand(viewModel, "CancelCommand");
        };

        WireCloseRequested(viewModel, session);
    }

    private void WireCloseRequested(object viewModel, DialogSession session)
    {
        var closeRequestedEvent = viewModel.GetType().GetEvent("CloseRequested");
        if (closeRequestedEvent == null)
            return;

        if (closeRequestedEvent.EventHandlerType == typeof(EventHandler<DialogCloseEventArgs>))
        {
            EventHandler<DialogCloseEventArgs> handler = (_, e) =>
            {
                if (e.DialogResult == true)
                    session.CloseApproved = true;

                Complete(
                    session,
                    e.DialogResult == true ? ContentDialogResult.Primary : ContentDialogResult.None);
            };
            closeRequestedEvent.AddEventHandler(viewModel, handler);
            return;
        }

        if (closeRequestedEvent.EventHandlerType == typeof(EventHandler))
        {
            EventHandler handler = (_, _) =>
            {
                var saved = viewModel is IWindowResult { Result: not null };
                if (saved)
                    session.CloseApproved = true;

                Complete(session, saved ? ContentDialogResult.Primary : ContentDialogResult.None);
            };
            closeRequestedEvent.AddEventHandler(viewModel, handler);
        }
    }

    private void Complete(DialogSession session, ContentDialogResult result)
    {
        if (session.Completion.Task.IsCompleted)
            return;

        session.Completion.TrySetResult(result);
    }

    private static void InvokeCommand(object viewModel, string commandPropertyName)
    {
        var command = viewModel.GetType().GetProperty(commandPropertyName)?.GetValue(viewModel) as ICommand;
        command?.Execute(null);
    }

    private sealed class DialogSession(
        ContentDialog dialog,
        TaskCompletionSource<ContentDialogResult> completion)
    {
        public ContentDialog Dialog { get; } = dialog;

        public TaskCompletionSource<ContentDialogResult> Completion { get; } = completion;

        public bool CloseApproved { get; set; }
    }
}
