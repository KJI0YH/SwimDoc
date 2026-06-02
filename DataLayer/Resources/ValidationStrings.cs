using System.Globalization;
using System.Resources;

namespace DataLayer.Resources;

public static class ValidationStrings
{
    private static readonly ResourceManager ResourceManagerImpl =
        new("DataLayer.Resources.ValidationStrings", typeof(ValidationStrings).Assembly);

    public static ResourceManager ResourceManager => ResourceManagerImpl;

    public static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? $"[[{name}]]";

    // SwimEvent
    public static string SwimEvent_OrderAndDateAlreadyExists_Format => Get(nameof(SwimEvent_OrderAndDateAlreadyExists_Format));
    public static string SwimEvent_AlreadyExists_ByStyleAgeGroupRound => Get(nameof(SwimEvent_AlreadyExists_ByStyleAgeGroupRound));
    public static string SwimEvent_SwimStyleNotSelected => Get(nameof(SwimEvent_SwimStyleNotSelected));
    public static string SwimEvent_AgeGroupNotSelected => Get(nameof(SwimEvent_AgeGroupNotSelected));
    public static string SwimEvent_RoundParticipantsCountMustBeGreaterThanZero => Get(nameof(SwimEvent_RoundParticipantsCountMustBeGreaterThanZero));
    public static string SwimEvent_CustomLaneNamesMustBeUnique => Get(nameof(SwimEvent_CustomLaneNamesMustBeUnique));
    public static string SwimEvent_InvalidLaneRange => Get(nameof(SwimEvent_InvalidLaneRange));
    public static string SwimEvent_InvalidRoundOrder => Get(nameof(SwimEvent_InvalidRoundOrder));

    // SwimStyle
    public static string SwimStyle_AlreadyExists_Format => Get(nameof(SwimStyle_AlreadyExists_Format));
    public static string SwimStyle_DistanceMustBeGreaterThanZero => Get(nameof(SwimStyle_DistanceMustBeGreaterThanZero));

    // AgeGroup
    public static string AgeGroup_InvalidYearRange => Get(nameof(AgeGroup_InvalidYearRange));
    public static string AgeGroup_AlreadyExists => Get(nameof(AgeGroup_AlreadyExists));

    // Athlete
    public static string Athlete_FirstNameCannotBeEmpty => Get(nameof(Athlete_FirstNameCannotBeEmpty));
    public static string Athlete_LastNameCannotBeEmpty => Get(nameof(Athlete_LastNameCannotBeEmpty));
    public static string Athlete_GenderCannotBeMixed => Get(nameof(Athlete_GenderCannotBeMixed));
    public static string Athlete_YearOfBirthInvalid => Get(nameof(Athlete_YearOfBirthInvalid));

    // Club
    public static string Club_NameCannotBeEmpty => Get(nameof(Club_NameCannotBeEmpty));

    // Entry
    public static string Entry_ParticipantMustBeProvided => Get(nameof(Entry_ParticipantMustBeProvided));
    public static string Entry_SwimStyleMustBeProvided => Get(nameof(Entry_SwimStyleMustBeProvided));
    public static string Entry_AlreadyExists => Get(nameof(Entry_AlreadyExists));
    public static string Entry_InvalidParticipant => Get(nameof(Entry_InvalidParticipant));
    public static string Entry_AthleteNotInAgeGroup => Get(nameof(Entry_AthleteNotInAgeGroup));
    public static string Entry_SwimEventMustBeIndividual => Get(nameof(Entry_SwimEventMustBeIndividual));
    public static string Entry_SwimEventMustBeRelay => Get(nameof(Entry_SwimEventMustBeRelay));

    // HeatPosition
    public static string HeatPosition_HeatDoesNotExist => Get(nameof(HeatPosition_HeatDoesNotExist));
    public static string HeatPosition_LaneOutOfRange_Format => Get(nameof(HeatPosition_LaneOutOfRange_Format));
    public static string HeatPosition_LaneAlreadyBusy_Format => Get(nameof(HeatPosition_LaneAlreadyBusy_Format));
    public static string HeatPosition_EntryAlreadyExists_Format => Get(nameof(HeatPosition_EntryAlreadyExists_Format));
}

