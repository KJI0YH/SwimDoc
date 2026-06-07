using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class EntityDisplay
{
    public static string FormatAthleteName(Athlete? athlete) =>
        athlete is null ? string.Empty : $"{athlete.FirstName} {athlete.LastName}";

    public static string FormatAthleteClubName(Athlete? athlete, IEntityDisplayTexts texts) =>
        athlete?.Club?.Name ?? texts.PersonalParen;

    public static int FormatAthletePointCount(Athlete? athlete) =>
        athlete?.Entries?.Where(e => e.Scoring).Sum(e => e.Points ?? 0) ?? 0;

    public static int FormatClubAthleteCount(Club? club) => club?.Athletes.Count ?? 0;

    public static int FormatClubRelayCount(Club? club) => club?.Relays.Count ?? 0;

    public static int FormatClubEntryCount(Club? club) =>
        club?.Athletes.Sum(a => a.Entries.Count) ?? 0;

    public static int FormatClubPointCount(Club? club) =>
        club?.Athletes.Sum(a => a.Entries.Where(e => e.Scoring).Sum(e => e.Points ?? 0)) ?? 0;

    public static string FormatRelayName(Relay? relay, IEntityDisplayTexts texts)
    {
        if (relay is null)
            return string.Empty;

        var clubName = relay.Club?.Name ?? texts.PersonalParen;
        var numberPart = relay.Number.HasValue ? relay.Number.ToString() : string.Empty;
        return $"{clubName} {numberPart}".Trim();
    }

    public static string FormatRelayNameWithAthletes(Relay? relay, IEntityDisplayTexts texts)
    {
        if (relay is null)
            return string.Empty;

        var athleteNames = relay.Positions?
            .OrderBy(p => p.Order)
            .Select(p => FormatAthleteName(p.Athlete))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList() ?? [];

        return athleteNames.Count == 0 ? FormatRelayName(relay, texts) : $"{FormatRelayName(relay, texts)} ({string.Join(", ", athleteNames)})";
    }

    public static string FormatEntryParticipantName(Entry? entry, IEntityDisplayTexts texts)
    {
        if (entry is null)
            return string.Empty;

        if (entry.Athlete is not null)
            return FormatAthleteName(entry.Athlete);

        if (entry.Relay is not null)
            return FormatRelayNameWithAthletes(entry.Relay, texts);

        return texts.MissingParticipantParen;
    }

    public static string FormatEntryParticipantClubName(Entry? entry, IEntityDisplayTexts texts)
    {
        if (entry is null)
            return string.Empty;

        if (entry.Athlete is not null)
            return FormatAthleteClubName(entry.Athlete, texts);

        return entry.Relay?.Club?.Name ?? texts.PersonalParen;
    }

    public static string FormatEntryParticipantBirthYear(Entry? entry, IEntityDisplayTexts texts)
    {
        if (entry is null)
            return string.Empty;

        if (entry.Athlete is not null)
            return entry.Athlete.YearOfBirth.ToString(texts.Culture);

        if (entry.Relay is not null)
            return FormatRelayBirthYears(entry.Relay, texts);

        return string.Empty;
    }

    public static string FormatEntryTime(Entry? entry) =>
        entry is null ? string.Empty : EntryTimeDisplay.FormatEntryTime(entry.EntryTime);

    public static string FormatFinishTime(Entry? entry) =>
        entry is null ? string.Empty : EntryTimeDisplay.FormatFinishTime(entry);

    public static string FormatSwimStyle(SwimStyle? style, IEntityDisplayTexts texts)
    {
        if (style == null)
            return string.Empty;

        var relayPrefix = style.IsRelay ? $"{style.RelayCount}x" : string.Empty;
        return string.Format(
            texts.Culture,
            texts.SwimStyleDisplayNameFormat,
            relayPrefix,
            style.Distance,
            texts.GetEnumDisplay(style.Stroke));
    }

    public static string FormatAgeGroup(AgeGroup? ageGroup, IEntityDisplayTexts texts)
    {
        if (ageGroup == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(ageGroup.Name))
            return ageGroup.Name;

        var yearPart = ageGroup.BirthYearMin == null && ageGroup.BirthYearMax == null
            ? texts.AgeGroupYearRangeOpenWithSuffix
            : string.Format(
                texts.Culture,
                texts.AgeGroupYearRangeWithSuffixFormat,
                FormatAgeGroupYearRange(ageGroup, texts));

        return string.Format(
            texts.Culture,
            texts.AgeGroupDisplayNameFormat,
            texts.GetEnumDisplay(ageGroup.Gender),
            yearPart);
    }

    public static string FormatSwimEvent(SwimEvent? swimEvent, IEntityDisplayTexts texts)
    {
        if (swimEvent == null)
            return string.Empty;

        return string.Format(
            texts.Culture,
            texts.SwimEventDisplayNameFormat,
            swimEvent.Order,
            texts.GetEnumDisplay(swimEvent.Round),
            FormatSwimStyle(swimEvent.SwimStyle, texts),
            FormatAgeGroup(swimEvent.AgeGroup, texts));
    }

    public static string FormatSwimEventDate(SwimEvent? swimEvent, IEntityDisplayTexts texts) =>
        swimEvent?.Date.ToString("d", texts.Culture) ?? string.Empty;

    public static string FormatSwimEventTime(SwimEvent? swimEvent) =>
        swimEvent is null ? string.Empty : StartTimeDisplay.Format(swimEvent.Time);

    public static string FormatSwimEventLanes(SwimEvent? swimEvent) =>
        swimEvent is null ? string.Empty : SwimEventLaneNames.FormatLanesSummary(swimEvent);

    public static string FormatHeatDayTime(Heat? heat) =>
        heat is null ? string.Empty : HeatDisplay.FormatDayTime(heat.DayTime);

    public static string FormatHeatNumberWithTime(Heat? heat) =>
        heat is null ? string.Empty : HeatDisplay.FormatNumberWithTime(heat);

    public static string FormatEntrySwimName(Entry? entry, IEntityDisplayTexts texts)
    {
        if (entry == null)
            return string.Empty;

        return entry.SwimEvent is not null
            ? FormatSwimEvent(entry.SwimEvent, texts)
            : FormatSwimStyle(entry.SwimStyle, texts);
    }

    private static string FormatRelayBirthYears(Relay relay, IEntityDisplayTexts texts)
    {
        var years = relay.Positions?
            .OrderBy(p => p.Order)
            .Select(p => p.Athlete?.YearOfBirth)
            .Where(year => year.HasValue)
            .Select(year => year!.Value.ToString(texts.Culture))
            .ToList() ?? [];

        return years.Count == 0 ? string.Empty : string.Join(", ", years);
    }

    private static string FormatAgeGroupYearRange(AgeGroup ageGroup, IEntityDisplayTexts texts)
    {
        if (ageGroup.BirthYearMin == null && ageGroup.BirthYearMax == null)
            return texts.AgeGroupYearRangeOpen;

        if (ageGroup.BirthYearMin == ageGroup.BirthYearMax && ageGroup.BirthYearMin.HasValue)
            return ageGroup.BirthYearMin.Value.ToString(texts.Culture);

        var minLabel = ageGroup.BirthYearMin.HasValue
            ? ageGroup.BirthYearMin.Value.ToString(texts.Culture)
            : texts.AgeGroupYearRangeOlderThan;

        var maxLabel = ageGroup.BirthYearMax.HasValue
            ? ageGroup.BirthYearMax.Value.ToString(texts.Culture)
            : texts.AgeGroupYearRangeYoungerThan;

        return $"{minLabel}-{maxLabel}";
    }
}
