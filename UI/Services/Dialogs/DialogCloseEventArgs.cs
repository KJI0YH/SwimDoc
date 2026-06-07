namespace UI.Services.Dialogs;

public sealed class DialogCloseEventArgs(bool? dialogResult) : EventArgs
{
    public bool? DialogResult { get; } = dialogResult;
}
