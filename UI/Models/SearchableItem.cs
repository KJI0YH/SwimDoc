using System.Collections.ObjectModel;
using DataLayer.EfClasses;
using UI.Helpers.Display;

namespace UI.Models;

public class SearchableItem
{
    public object? Value { get; set; }
    public string DisplayText { get; set; } = string.Empty;

    public static IEnumerable<SearchableItem> FromSwimEvents(
        IEnumerable<SwimEvent> swimEvents,
        Func<SwimEvent, string>? formatDisplay = null)
    {
        formatDisplay ??= EntityDisplayFormatter.FormatSwimEvent;
        return swimEvents
            .OrderBy(swimEvent => swimEvent.Order)
            .ThenBy(swimEvent => swimEvent.Date)
            .Select(swimEvent => new SearchableItem
            {
                Value = swimEvent,
                DisplayText = formatDisplay(swimEvent)
            });
    }

    public static ObservableCollection<SearchableItem> ToSwimEventOptions(
        IEnumerable<SwimEvent> swimEvents,
        Func<SwimEvent, string>? formatDisplay = null) =>
        new(FromSwimEvents(swimEvents, formatDisplay));

    public override string ToString() => DisplayText;
}
