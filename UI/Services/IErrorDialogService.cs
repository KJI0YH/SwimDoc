namespace UI.Services;

public interface IErrorDialogService
{
    Task ShowErrorAsync(string title, string message, CancellationToken cancellationToken = default);
}
