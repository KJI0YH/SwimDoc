using System.Globalization;
using DataLayer.Display;
using UI.Resources;

namespace UI.Helpers.Display;

internal sealed class UiEntityDisplayTexts : IEntityDisplayTexts
{
    public static readonly UiEntityDisplayTexts Instance = new();
    public CultureInfo Culture => CultureInfo.CurrentUICulture;
    public string GetEnumDisplay(Enum value) => Strings.GetEnumDisplay(value);
    public string SwimStyleDisplayNameFormat => Strings.Get("SwimStyle_DisplayName_Format");
    public string SwimEventDisplayNameFormat => Strings.Get("SwimEvent_DisplayName_Format");
    public string AgeGroupDisplayNameFormat => Strings.Get("AgeGroup_DisplayName_Format");
    public string AgeGroupYearRangeOpen => Strings.Get("AgeGroup_YearRange_Open");
    public string AgeGroupYearRangeOpenWithSuffix => Strings.Get("AgeGroup_YearRange_OpenWithSuffix");
    public string AgeGroupYearRangeWithSuffixFormat => Strings.Get("AgeGroup_YearRange_WithSuffix_Format");
    public string AgeGroupYearRangeOlderThan => Strings.Get("AgeGroup_YearRange_OlderThan");
    public string AgeGroupYearRangeYoungerThan => Strings.Get("AgeGroup_YearRange_YoungerThan");
    public string PersonalParen => Strings.Common_PersonalParen;
    public string MissingParticipantParen => string.Empty;
}
