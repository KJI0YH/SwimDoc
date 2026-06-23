using System.Windows;
using System.Windows.Controls;

namespace UI.Helpers.Dialogs;

public static class DialogContentFactory
{
    public const double DefaultWidth = 520;
    public const double MessageWidth = 420;

    public static TextBlock CreateMessageContent(string text, double width = MessageWidth) =>
        new()
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Width = width,
            MinWidth = width,
            MaxWidth = width,
        };
}
