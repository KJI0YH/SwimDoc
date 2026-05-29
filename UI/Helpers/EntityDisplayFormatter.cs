using DataLayer.EfClasses;
using UI.Resources;

namespace UI.Helpers;

public static class EntityDisplayFormatter
{
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
