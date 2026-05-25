using CommunityToolkit.Mvvm.ComponentModel;

namespace UI.ViewModels.Pages;

public interface IEventFilterOption
{
    bool IsSelected { get; set; }
}

public sealed partial class EventFilterOption<T>(T value, string displayText) : ObservableObject, IEventFilterOption
{
    [ObservableProperty] private bool _isSelected;

    public T Value { get; } = value;
    public string DisplayText { get; } = displayText;

    public override string ToString() => DisplayText;
}
