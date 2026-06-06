using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace UI.Views.Controls.AddEditView;

/// <summary>
/// Holds multiple <see cref="ContentDialog"/> layers. Only the top dialog is visible;
/// parents stay in the visual tree but are hidden while a child is open.
/// </summary>
public sealed class DialogStackHost : Grid
{
    private readonly List<ContentDialog> _layers = [];

    public int Count => _layers.Count;

    public ContentDialog? Top => _layers.Count > 0 ? _layers[^1] : null;

    public void Push(ContentDialog dialog)
    {
        if (_layers.Count > 0)
            _layers[^1].Visibility = Visibility.Collapsed;

        dialog.Visibility = Visibility.Visible;
        _layers.Add(dialog);
        Children.Add(dialog);
        Panel.SetZIndex(dialog, _layers.Count);
    }

    public void Remove(ContentDialog dialog)
    {
        var index = _layers.IndexOf(dialog);
        if (index < 0)
            return;

        _layers.RemoveAt(index);
        Children.Remove(dialog);

        for (var i = 0; i < _layers.Count; i++)
            Panel.SetZIndex(_layers[i], i + 1);

        if (_layers.Count > 0)
            _layers[^1].Visibility = Visibility.Visible;
    }
}
