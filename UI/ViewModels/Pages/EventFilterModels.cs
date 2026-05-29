using CommunityToolkit.Mvvm.ComponentModel;

namespace UI.ViewModels.Pages;

public interface IEventFilterOption
{
    bool IsSelected { get; set; }
}

public sealed partial class EventFilterOption<T>(T value, string displayText) : ObservableObject, IEventFilterOption
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string _displayText = displayText;

    public T Value { get; } = value;

    public override string ToString() => DisplayText;
}
