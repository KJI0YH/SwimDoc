using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace UI.Helpers.Dialogs;

public static class DialogContentFactory
{
    public const double DefaultWidth = 520;
    public const double MessageWidth = 420;

    public static TextBlock CreateMessageContent(string text, double width = MessageWidth)
    {
        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Width = width,
            MinWidth = width,
            MaxWidth = width,
        };

        foreach (var line in NormalizeNewLines(text).Split('\n'))
        {
            if (textBlock.Inlines.Count > 0)
                textBlock.Inlines.Add(new LineBreak());

            textBlock.Inlines.Add(new Run(line));
        }

        return textBlock;
    }

    private static string NormalizeNewLines(string text) =>
        text.Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("\\n", "\n");
}
