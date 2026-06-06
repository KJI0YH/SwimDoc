using DataLayer;
using DataLayer.Display;
using DataLayer.EfClasses;
using UI.Resources;

namespace UI.Helpers;

public static class EntityDisplayFormatter
{
    public static string FormatAthleteName(Athlete? athlete) =>
        athlete is null ? string.Empty : $"{athlete.FirstName} {athlete.LastName}";

    public static string FormatAthleteClubName(Athlete? athlete) =>
        athlete?.Club?.Name ?? Strings.Common_PersonalParen;

    public static int FormatAthletePointCount(Athlete? athlete) =>
        athlete?.Entries?.Where(e => e.Scoring).Sum(e => e.Points ?? 0) ?? 0;

    public static int FormatClubAthleteCount(Club? club) => club?.Athletes.Count ?? 0;

    public static int FormatClubRelayCount(Club? club) => club?.Relays.Count ?? 0;

    public static int FormatClubEntryCount(Club? club) =>
        club?.Athletes.Sum(a => a.Entries.Count) ?? 0;

    public static int FormatClubPointCount(Club? club) =>
        club?.Athletes.Sum(a => a.Entries.Where(e => e.Scoring).Sum(e => e.Points ?? 0)) ?? 0;

    public static string FormatRelayName(Relay? relay)
    {
        if (relay is null)
            return string.Empty;

        var numberPart = relay.Number.HasValue ? relay.Number.ToString() : string.Empty;
        return $"{relay.Club.Name} {numberPart}".Trim();
    }

    public static string FormatRelayNameWithAthletes(Relay? relay)
    {
        if (relay is null)
            return string.Empty;

        var athleteNames = relay.Positions
            .OrderBy(p => p.Order)
            .Select(p => FormatAthleteName(p.Athlete));
        return $"{FormatRelayName(relay)} ({string.Join(", ", athleteNames)})";
    }

    public static string FormatEntryParticipantName(Entry? entry)
    {
        if (entry is null)
            return string.Empty;

        if (entry.Athlete is not null)
            return FormatAthleteName(entry.Athlete);

        return entry.Relay is not null ? FormatRelayNameWithAthletes(entry.Relay) : string.Empty;
    }

    public static string FormatEntryParticipantClubName(Entry? entry)
    {
        if (entry is null)
            return string.Empty;

        if (entry.Athlete is not null)
            return FormatAthleteClubName(entry.Athlete);

        return entry.Relay?.Club.Name ?? string.Empty;
    }

    public static string FormatEntryParticipantBirthYear(Entry? entry)
    {
        if (entry is null)
            return string.Empty;

        if (entry.Athlete is not null)
            return entry.Athlete.YearOfBirth.ToString();

        return entry.Relay is not null
            ? string.Join(", ", entry.Relay.Positions.OrderBy(p => p.Order).Select(p => p.Athlete.YearOfBirth))
            : string.Empty;
    }

    public static string FormatEntryTime(Entry? entry) =>
        entry is null ? string.Empty : EntryTimeDisplay.FormatEntryTime(entry.EntryTime);

    public static string FormatFinishTime(Entry? entry) =>
        entry is null ? string.Empty : EntryTimeDisplay.FormatFinishTime(entry);

    public static string FormatSwimStyle(SwimStyle? style)
    {
        if (style == null)
            return string.Empty;

        var relayPrefix = style.IsRelay ? $"{style.RelayCount}x" : string.Empty;
        return string.Format(
            Strings.Get("SwimStyle_DisplayName_Format"),
            relayPrefix,
            style.Distance,
            Strings.GetEnumDisplay(style.Stroke));
    }

    public static string FormatAgeGroup(AgeGroup? ageGroup)
    {
        if (ageGroup == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(ageGroup.Name))
            return ageGroup.Name;

        var yearPart = ageGroup.BirthYearMin == null && ageGroup.BirthYearMax == null
            ? Strings.Get("AgeGroup_YearRange_OpenWithSuffix")
            : string.Format(Strings.Get("AgeGroup_YearRange_WithSuffix_Format"), FormatAgeGroupYearRange(ageGroup));

        return string.Format(
            Strings.Get("AgeGroup_DisplayName_Format"),
            Strings.GetEnumDisplay(ageGroup.Gender),
            yearPart);
    }

    public static string FormatSwimEvent(SwimEvent? swimEvent)
    {
        if (swimEvent == null)
            return string.Empty;

        return string.Format(
            Strings.Get("SwimEvent_DisplayName_Format"),
            swimEvent.Order,
            Strings.GetEnumDisplay(swimEvent.Round),
            FormatSwimStyle(swimEvent.SwimStyle),
            FormatAgeGroup(swimEvent.AgeGroup));
    }

    public static string FormatSwimEventDate(SwimEvent? swimEvent) =>
        swimEvent?.Date.ToShortDateString() ?? string.Empty;

    public static string FormatSwimEventTime(SwimEvent? swimEvent) =>
        swimEvent is null ? string.Empty : StartTimeDisplay.Format(swimEvent.Time);

    public static string FormatSwimEventLanes(SwimEvent? swimEvent) =>
        swimEvent is null ? string.Empty : SwimEventLaneNames.FormatLanesSummary(swimEvent);

    public static string FormatSwimEventStatus(SwimEvent? swimEvent) =>
        swimEvent?.Status.ToString() ?? string.Empty;

    public static string FormatHeatDayTime(Heat? heat) =>
        heat is null ? string.Empty : HeatDisplay.FormatDayTime(heat.DayTime);

    public static string FormatHeatNumberWithTime(Heat? heat) =>
        heat is null ? string.Empty : HeatDisplay.FormatNumberWithTime(heat);

    public static string FormatEntrySwimName(Entry? entry)
    {
        if (entry == null)
            return string.Empty;

        return entry.SwimEvent is not null
            ? FormatSwimEvent(entry.SwimEvent)
            : FormatSwimStyle(entry.SwimStyle);
    }

    private static string FormatAgeGroupYearRange(AgeGroup ageGroup)
    {
        if (ageGroup.BirthYearMin == null && ageGroup.BirthYearMax == null)
            return Strings.Get("AgeGroup_YearRange_Open");

        if (ageGroup.BirthYearMin == ageGroup.BirthYearMax && ageGroup.BirthYearMin.HasValue)
            return ageGroup.BirthYearMin.Value.ToString();

        var minLabel = ageGroup.BirthYearMin.HasValue
            ? ageGroup.BirthYearMin.Value.ToString()
            : Strings.Get("AgeGroup_YearRange_OlderThan");
        var maxLabel = ageGroup.BirthYearMax.HasValue
            ? ageGroup.BirthYearMax.Value.ToString()
            : Strings.Get("AgeGroup_YearRange_YoungerThan");

        return $"{minLabel}-{maxLabel}";
    }
}
