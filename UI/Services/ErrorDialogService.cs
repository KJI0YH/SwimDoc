using Wpf.Ui;
using Wpf.Ui.Controls;
using UI.Resources;

namespace UI.Services;

public sealed class ErrorDialogService(IContentDialogService contentDialogService) : IErrorDialogService
{
    public async Task ShowErrorAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = Strings.Common_Ok,
            DefaultButton = ContentDialogButton.Close
        };

        _ = await contentDialogService.ShowAsync(dialog, cancellationToken);
    }
}
