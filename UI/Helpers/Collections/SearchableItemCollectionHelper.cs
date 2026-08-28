using System.Collections.ObjectModel;
using DataLayer.EfClasses;
using UI.Helpers.Display;
using UI.Models;
using UI.Resources;

namespace UI.Helpers.Collections;

public static class SearchableItemCollectionHelper
{
    public static SearchableItem? FindClub(ObservableCollection<SearchableItem> items, int? clubId) =>
        clubId is int id
            ? items.FirstOrDefault(item => item.Value is Club club && club.Id == id)
            : items.FirstOrDefault(item => item.Value is null);

    public static SearchableItem? FindAthlete(ObservableCollection<SearchableItem> items, int? athleteId) =>
        athleteId is int id
            ? items.FirstOrDefault(item => item.Value is Athlete athlete && athlete.Id == id)
            : null;

    public static SearchableItem? FindSwimEvent(ObservableCollection<SearchableItem> items, int? swimEventId) =>
        swimEventId is int id
            ? items.FirstOrDefault(item => item.Value is SwimEvent swimEvent && swimEvent.Id == id)
            : items.FirstOrDefault(item => item.Value is null);

    public static SearchableItem? FindAgeGroup(ObservableCollection<SearchableItem> items, int ageGroupId) =>
        items.FirstOrDefault(item => item.Value is AgeGroup ageGroup && ageGroup.Id == ageGroupId);

    public static SearchableItem? FindSwimStyle(ObservableCollection<SearchableItem> items, int swimStyleId) =>
        items.FirstOrDefault(item => item.Value is SwimStyle swimStyle && swimStyle.Id == swimStyleId);

    public static void EnsureClub(
        ObservableCollection<SearchableItem> items,
        Club? club,
        bool includePersonalOption)
    {
        if (includePersonalOption && items.All(item => item.Value is not null))
            items.Insert(0, new SearchableItem { Value = null, DisplayText = Strings.Common_PersonalParen });

        if (club is null)
            return;

        if (items.Any(item => item.Value is Club existing && existing.Id == club.Id))
            return;

        items.Add(new SearchableItem { Value = club, DisplayText = club.Name });
    }

    public static void EnsureAthlete(ObservableCollection<SearchableItem> items, Athlete? athlete)
    {
        if (athlete is null || items.Any(item => item.Value is Athlete existing && existing.Id == athlete.Id))
            return;

        items.Add(new SearchableItem
        {
            Value = athlete,
            DisplayText = EntityDisplayFormatter.FormatAthleteName(athlete)
        });
    }

    public static void EnsureSwimEvent(ObservableCollection<SearchableItem> items, SwimEvent? swimEvent)
    {
        if (swimEvent is null || items.Any(item => item.Value is SwimEvent existing && existing.Id == swimEvent.Id))
            return;

        items.Add(new SearchableItem
        {
            Value = swimEvent,
            DisplayText = EntityDisplayFormatter.FormatSwimEvent(swimEvent)
        });
    }

    public static void EnsureAgeGroup(ObservableCollection<SearchableItem> items, AgeGroup? ageGroup)
    {
        if (ageGroup is null || items.Any(item => item.Value is AgeGroup existing && existing.Id == ageGroup.Id))
            return;

        items.Add(new SearchableItem
        {
            Value = ageGroup,
            DisplayText = EntityDisplayFormatter.FormatAgeGroup(ageGroup)
        });
    }

    public static void EnsureSwimStyle(ObservableCollection<SearchableItem> items, SwimStyle? swimStyle)
    {
        if (swimStyle is null || items.Any(item => item.Value is SwimStyle existing && existing.Id == swimStyle.Id))
            return;

        items.Add(new SearchableItem
        {
            Value = swimStyle,
            DisplayText = EntityDisplayFormatter.FormatSwimStyle(swimStyle)
        });
    }

    public static void EnsureAthleteInList(List<Athlete> athletes, Athlete? athlete)
    {
        if (athlete is null || athletes.Any(existing => existing.Id == athlete.Id))
            return;
        athletes.Add(athlete);
    }

    public static void EnsureSwimEventInList(List<SwimEvent> swimEvents, SwimEvent? swimEvent)
    {
        if (swimEvent is null || swimEvents.Any(existing => existing.Id == swimEvent.Id))
            return;
        swimEvents.Add(swimEvent);
    }
}
