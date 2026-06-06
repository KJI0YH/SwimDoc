using System.Globalization;
using BizLogic.Resources;
using ReportExcelStrings = BizLogic.Resources.ReportExcelStrings;
using DataLayer;
using DataLayer.EfClasses;

namespace BizLogic.Helpers;

public static class LocalizedEntityDisplayFormatter
{
    public static string GetEnumDisplay(Enum value)
    {
        var key = $"Enum_{value.GetType().Name}_{value}";
        var localized = EntityDisplayStrings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        return string.IsNullOrWhiteSpace(localized) ? EnumDisplay.GetDescription(value) : localized;
    }

    public static string FormatSwimStyle(SwimStyle? style)
    {
        if (style == null)
            return string.Empty;

        var relayPrefix = style.IsRelay ? $"{style.RelayCount}x" : string.Empty;
        return string.Format(
            CultureInfo.CurrentUICulture,
            EntityDisplayStrings.SwimStyle_DisplayName_Format,
            relayPrefix,
            style.Distance,
            GetEnumDisplay(style.Stroke));
    }

    public static string FormatAgeGroup(AgeGroup? ageGroup)
    {
        if (ageGroup == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(ageGroup.Name))
            return ageGroup.Name;

        var yearPart = ageGroup.BirthYearMin == null && ageGroup.BirthYearMax == null
            ? EntityDisplayStrings.AgeGroup_YearRange_OpenWithSuffix
            : string.Format(
                CultureInfo.CurrentUICulture,
                EntityDisplayStrings.AgeGroup_YearRange_WithSuffix_Format,
                FormatAgeGroupYearRange(ageGroup));

        return string.Format(
            CultureInfo.CurrentUICulture,
            EntityDisplayStrings.AgeGroup_DisplayName_Format,
            GetEnumDisplay(ageGroup.Gender),
            yearPart);
    }

    public static string FormatAthleteName(Athlete? athlete) =>
        athlete is null ? string.Empty : $"{athlete.FirstName} {athlete.LastName}";

    public static string FormatAthleteClubName(Athlete? athlete) =>
        athlete?.Club?.Name ?? ReportExcelStrings.Value_PersonalParen;

    public static string FormatSwimEvent(SwimEvent? swimEvent)
    {
        if (swimEvent == null)
            return string.Empty;

        return string.Format(
            CultureInfo.CurrentUICulture,
            EntityDisplayStrings.SwimEvent_DisplayName_Format,
            swimEvent.Order,
            GetEnumDisplay(swimEvent.Round),
            FormatSwimStyle(swimEvent.SwimStyle),
            FormatAgeGroup(swimEvent.AgeGroup));
    }

    private static string FormatAgeGroupYearRange(AgeGroup ageGroup)
    {
        if (ageGroup.BirthYearMin == null && ageGroup.BirthYearMax == null)
            return EntityDisplayStrings.AgeGroup_YearRange_Open;

        if (ageGroup.BirthYearMin == ageGroup.BirthYearMax && ageGroup.BirthYearMin.HasValue)
            return ageGroup.BirthYearMin.Value.ToString(CultureInfo.CurrentUICulture);

        var minLabel = ageGroup.BirthYearMin.HasValue
            ? ageGroup.BirthYearMin.Value.ToString(CultureInfo.CurrentUICulture)
            : EntityDisplayStrings.AgeGroup_YearRange_OlderThan;

        var maxLabel = ageGroup.BirthYearMax.HasValue
            ? ageGroup.BirthYearMax.Value.ToString(CultureInfo.CurrentUICulture)
            : EntityDisplayStrings.AgeGroup_YearRange_YoungerThan;

        return $"{minLabel}-{maxLabel}";
    }
}
