namespace UI.Services;

public sealed class AddEditDialogResult(bool? dialogResult, object? dataContext)
{
    public bool? DialogResult { get; } = dialogResult;

    public object? DataContext { get; } = dataContext;
}
