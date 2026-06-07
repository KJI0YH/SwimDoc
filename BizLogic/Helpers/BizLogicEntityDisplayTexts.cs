using System.Globalization;
using BizLogic.Resources;
using DataLayer.Display;

namespace BizLogic.Helpers;

internal sealed class BizLogicEntityDisplayTexts : IEntityDisplayTexts
{
    public static readonly BizLogicEntityDisplayTexts Instance = new();

    public CultureInfo Culture => CultureInfo.CurrentUICulture;

    public string GetEnumDisplay(Enum value)
    {
        var key = $"Enum_{value.GetType().Name}_{value}";
        var localized = EntityDisplayStrings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        return string.IsNullOrWhiteSpace(localized) ? EnumDisplay.GetDescription(value) : localized;
    }

    public string SwimStyleDisplayNameFormat => EntityDisplayStrings.SwimStyle_DisplayName_Format;

    public string SwimEventDisplayNameFormat => EntityDisplayStrings.SwimEvent_DisplayName_Format;

    public string AgeGroupDisplayNameFormat => EntityDisplayStrings.AgeGroup_DisplayName_Format;

    public string AgeGroupYearRangeOpen => EntityDisplayStrings.AgeGroup_YearRange_Open;

    public string AgeGroupYearRangeOpenWithSuffix => EntityDisplayStrings.AgeGroup_YearRange_OpenWithSuffix;

    public string AgeGroupYearRangeWithSuffixFormat => EntityDisplayStrings.AgeGroup_YearRange_WithSuffix_Format;

    public string AgeGroupYearRangeOlderThan => EntityDisplayStrings.AgeGroup_YearRange_OlderThan;

    public string AgeGroupYearRangeYoungerThan => EntityDisplayStrings.AgeGroup_YearRange_YoungerThan;

    public string PersonalParen => ReportExcelStrings.Value_PersonalParen;

    public string MissingParticipantParen => ReportExcelStrings.Value_NoneParen;
}
