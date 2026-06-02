using System.Globalization;
using System.Resources;

namespace ServiceLayer.Resources;

public static class ServiceErrorStrings
{
    private static readonly ResourceManager ResourceManagerImpl =
        new("ServiceLayer.Resources.ServiceErrorStrings", typeof(ServiceErrorStrings).Assembly);

    public static ResourceManager ResourceManager => ResourceManagerImpl;

    public static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? $"[[{name}]]";

    public static string Entry_Copy_RoundParticipantsCountMissing => Get(nameof(Entry_Copy_RoundParticipantsCountMissing));
    public static string Entry_Copy_PreviousEventMustBeOfficial => Get(nameof(Entry_Copy_PreviousEventMustBeOfficial));
    public static string Entry_Copy_NoFinishersWithTime => Get(nameof(Entry_Copy_NoFinishersWithTime));
    public static string Entry_Copy_EventNotFound_Format => Get(nameof(Entry_Copy_EventNotFound_Format));

    public static string Heat_Number_MinOne => Get(nameof(Heat_Number_MinOne));
    public static string Heat_Save_SwimEventNotFound_Format => Get(nameof(Heat_Save_SwimEventNotFound_Format));
    public static string Heat_Save_HeatNotFound_Format => Get(nameof(Heat_Save_HeatNotFound_Format));
    public static string Heat_Validation_MaxPositions_Format => Get(nameof(Heat_Validation_MaxPositions_Format));
    public static string Heat_Validation_DuplicateLane_Format => Get(nameof(Heat_Validation_DuplicateLane_Format));
    public static string Heat_Validation_DuplicateEntry_Format => Get(nameof(Heat_Validation_DuplicateEntry_Format));
    public static string Heat_Validation_LaneOutOfRange_Format => Get(nameof(Heat_Validation_LaneOutOfRange_Format));
    public static string Heat_Validation_EntryNotInEvent_Format => Get(nameof(Heat_Validation_EntryNotInEvent_Format));
    public static string Heat_Validation_EntryAlreadyInOtherHeat_Format => Get(nameof(Heat_Validation_EntryAlreadyInOtherHeat_Format));
    public static string Heat_Approve_PositionsMissing => Get(nameof(Heat_Approve_PositionsMissing));
    public static string Heat_Approve_NoResultForEntry_Format => Get(nameof(Heat_Approve_NoResultForEntry_Format));
    public static string Heat_Approve_NotAllLaneResultsProvided => Get(nameof(Heat_Approve_NotAllLaneResultsProvided));

    public static string ReportExport_NoSwimEventsSelected => Get(nameof(ReportExport_NoSwimEventsSelected));
    public static string ReportExport_OutputPathEmpty => Get(nameof(ReportExport_OutputPathEmpty));
    public static string ReportExport_NoReportsSelected => Get(nameof(ReportExport_NoReportsSelected));
}

