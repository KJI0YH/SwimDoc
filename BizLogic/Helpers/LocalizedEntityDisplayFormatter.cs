using DataLayer.Display;
using DataLayer.EfClasses;

namespace BizLogic.Helpers;

public static class LocalizedEntityDisplayFormatter
{
    private static readonly IEntityDisplayTexts Texts = BizLogicEntityDisplayTexts.Instance;

    public static string GetEnumDisplay(Enum value) => Texts.GetEnumDisplay(value);

    public static string FormatSwimStyle(SwimStyle? style) =>
        EntityDisplay.FormatSwimStyle(style, Texts);

    public static string FormatAgeGroup(AgeGroup? ageGroup) =>
        EntityDisplay.FormatAgeGroup(ageGroup, Texts);

    public static string FormatAthleteName(Athlete? athlete) =>
        EntityDisplay.FormatAthleteName(athlete);

    public static string FormatAthleteClubName(Athlete? athlete) =>
        EntityDisplay.FormatAthleteClubName(athlete, Texts);

    public static string FormatSwimEvent(SwimEvent? swimEvent) =>
        EntityDisplay.FormatSwimEvent(swimEvent, Texts);

    public static string FormatEntryParticipantName(Entry entry) =>
        EntityDisplay.FormatEntryParticipantName(entry, Texts);

    public static string FormatEntryParticipantBirthYear(Entry entry) =>
        EntityDisplay.FormatEntryParticipantBirthYear(entry, Texts);

    public static string FormatEntryParticipantClubName(Entry entry) =>
        EntityDisplay.FormatEntryParticipantClubName(entry, Texts);
}
