using CommunityToolkit.Mvvm.ComponentModel;

namespace UI.Models.Filters;

public sealed partial class EventFilterOption<T>(T value, string displayText) : ObservableObject, IEventFilterOption
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string _displayText = displayText;

    public T Value { get; } = value;

    public override string ToString() => DisplayText;
}
