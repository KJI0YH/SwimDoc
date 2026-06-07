using DataLayer.Display;
using DataLayer.EfClasses;
using UI.Resources;

namespace UI.Helpers.Display;

public static class EntityDisplayFormatter
{
    private static readonly IEntityDisplayTexts Texts = UiEntityDisplayTexts.Instance;

    public static string FormatAthleteName(Athlete? athlete) =>
        EntityDisplay.FormatAthleteName(athlete);

    public static string FormatAthleteClubName(Athlete? athlete) =>
        EntityDisplay.FormatAthleteClubName(athlete, Texts);

    public static string FormatAthleteCategory(Athlete? athlete) =>
        athlete is null ? string.Empty : Strings.GetEnumDisplay(athlete.Category);

    public static int FormatAthletePointCount(Athlete? athlete) =>
        EntityDisplay.FormatAthletePointCount(athlete);

    public static int FormatClubAthleteCount(Club? club) =>
        EntityDisplay.FormatClubAthleteCount(club);

    public static int FormatClubRelayCount(Club? club) =>
        EntityDisplay.FormatClubRelayCount(club);

    public static int FormatClubEntryCount(Club? club) =>
        EntityDisplay.FormatClubEntryCount(club);

    public static int FormatClubPointCount(Club? club) =>
        EntityDisplay.FormatClubPointCount(club);

    public static string FormatRelayName(Relay? relay) =>
        EntityDisplay.FormatRelayName(relay, Texts);

    public static string FormatRelayNameWithAthletes(Relay? relay) =>
        EntityDisplay.FormatRelayNameWithAthletes(relay, Texts);

    public static string FormatEntryParticipantName(Entry? entry) =>
        EntityDisplay.FormatEntryParticipantName(entry, Texts);

    public static string FormatEntryParticipantClubName(Entry? entry) =>
        EntityDisplay.FormatEntryParticipantClubName(entry, Texts);

    public static string FormatEntryParticipantBirthYear(Entry? entry) =>
        EntityDisplay.FormatEntryParticipantBirthYear(entry, Texts);

    public static string FormatEntryTime(Entry? entry) =>
        EntityDisplay.FormatEntryTime(entry);

    public static string FormatFinishTime(Entry? entry) =>
        EntityDisplay.FormatFinishTime(entry);

    public static string FormatSwimStyle(SwimStyle? style) =>
        EntityDisplay.FormatSwimStyle(style, Texts);

    public static string FormatAgeGroup(AgeGroup? ageGroup) =>
        EntityDisplay.FormatAgeGroup(ageGroup, Texts);

    public static string FormatSwimEvent(SwimEvent? swimEvent) =>
        EntityDisplay.FormatSwimEvent(swimEvent, Texts);

    public static string FormatSwimEventDate(SwimEvent? swimEvent) =>
        EntityDisplay.FormatSwimEventDate(swimEvent, Texts);

    public static string FormatSwimEventTime(SwimEvent? swimEvent) =>
        EntityDisplay.FormatSwimEventTime(swimEvent);

    public static string FormatSwimEventLanes(SwimEvent? swimEvent) =>
        EntityDisplay.FormatSwimEventLanes(swimEvent);

    public static string FormatSwimEventStatus(SwimEvent? swimEvent) =>
        swimEvent?.Status.ToString() ?? string.Empty;

    public static string FormatHeatDayTime(Heat? heat) =>
        EntityDisplay.FormatHeatDayTime(heat);

    public static string FormatHeatNumberWithTime(Heat? heat) =>
        EntityDisplay.FormatHeatNumberWithTime(heat);

    public static string FormatEntrySwimName(Entry? entry) =>
        EntityDisplay.FormatEntrySwimName(entry, Texts);
}
