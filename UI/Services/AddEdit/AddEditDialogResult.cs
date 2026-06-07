namespace UI.Services.AddEdit;

public sealed class AddEditDialogResult(bool? dialogResult, object? dataContext)
{
    public bool? DialogResult { get; } = dialogResult;
    public object? DataContext { get; } = dataContext;
}
