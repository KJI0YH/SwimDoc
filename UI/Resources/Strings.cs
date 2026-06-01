using System.Globalization;
using System.Resources;
using DataLayer;

namespace UI.Resources;

public static class Strings
{
    private static readonly ResourceManager ResourceManagerImpl =
        new("UI.Resources.Strings", typeof(Strings).Assembly);

    public static ResourceManager ResourceManager => ResourceManagerImpl;

    public static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? $"[[{name}]]";

    public static string GetEnumDisplay(Enum value)
    {
        var key = $"Enum_{value.GetType().Name}_{value}";
        var localized = ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        return string.IsNullOrWhiteSpace(localized) ? EnumDisplay.GetDescription(value) : localized;
    }

    public static bool TryFindEnumByDisplayContains<T>(string search, out T enumValue) where T : struct, Enum
    {
        enumValue = default;
        if (string.IsNullOrWhiteSpace(search))
            return false;

        foreach (T item in Enum.GetValues<T>())
        {
            var display = GetEnumDisplay(item);
            if (display.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                item.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                enumValue = item;
                return true;
            }
        }

        return false;
    }

    public static string Enum_Gender_Male => Get(nameof(Enum_Gender_Male));
    public static string Enum_Gender_Female => Get(nameof(Enum_Gender_Female));
    public static string Enum_Gender_Mixed => Get(nameof(Enum_Gender_Mixed));
    public static string Enum_Stroke_Fly => Get(nameof(Enum_Stroke_Fly));
    public static string Enum_Stroke_Back => Get(nameof(Enum_Stroke_Back));
    public static string Enum_Stroke_Breast => Get(nameof(Enum_Stroke_Breast));
    public static string Enum_Stroke_Free => Get(nameof(Enum_Stroke_Free));
    public static string Enum_Stroke_Medley => Get(nameof(Enum_Stroke_Medley));

    public static string Nav_Program => Get(nameof(Nav_Program));
    public static string Nav_Entries => Get(nameof(Nav_Entries));
    public static string Nav_Heats => Get(nameof(Nav_Heats));
    public static string Nav_Fixation => Get(nameof(Nav_Fixation));
    public static string Nav_Results => Get(nameof(Nav_Results));
    public static string Nav_Athletes => Get(nameof(Nav_Athletes));
    public static string Nav_Clubs => Get(nameof(Nav_Clubs));
    public static string Nav_AgeGroups => Get(nameof(Nav_AgeGroups));
    public static string Nav_SwimStyles => Get(nameof(Nav_SwimStyles));
    public static string Nav_Settings => Get(nameof(Nav_Settings));
    public static string Nav_About => Get(nameof(Nav_About));

    public static string Menu_Competition => Get(nameof(Menu_Competition));
    public static string Menu_Create => Get(nameof(Menu_Create));
    public static string Menu_Open => Get(nameof(Menu_Open));

    public static string Settings_Tab_BaseTime => Get(nameof(Settings_Tab_BaseTime));
    public static string Settings_Tab_Templates => Get(nameof(Settings_Tab_Templates));
    public static string Settings_Page_Subtitle => Get(nameof(Settings_Page_Subtitle));
    public static string Settings_Section_General => Get(nameof(Settings_Section_General));
    public static string Settings_Section_Files => Get(nameof(Settings_Section_Files));
    public static string Settings_Section_Competition => Get(nameof(Settings_Section_Competition));
    public static string Settings_Language_Description => Get(nameof(Settings_Language_Description));
    public static string Settings_BaseTimes_Description => Get(nameof(Settings_BaseTimes_Description));
    public static string Settings_Section_Paging => Get(nameof(Settings_Section_Paging));
    public static string Settings_Paging_Description => Get(nameof(Settings_Paging_Description));
    public static string Settings_Paging_PageSize => Get(nameof(Settings_Paging_PageSize));
    public static string Common_Back => Get(nameof(Common_Back));
    public static string BaseTimes_WorldAquaticsPoints => Get(nameof(BaseTimes_WorldAquaticsPoints));
    public static string BaseTimes_WorldAquaticsPoints_Tooltip => Get(nameof(BaseTimes_WorldAquaticsPoints_Tooltip));
    public static string BaseTimes_Group_Scm => Get(nameof(BaseTimes_Group_Scm));
    public static string BaseTimes_Group_ScmMixedRelay => Get(nameof(BaseTimes_Group_ScmMixedRelay));
    public static string BaseTimes_Group_Lcm => Get(nameof(BaseTimes_Group_Lcm));
    public static string BaseTimes_Group_LcmMixedRelay => Get(nameof(BaseTimes_Group_LcmMixedRelay));
    public static string BaseTimes_Col_Stroke => Get(nameof(BaseTimes_Col_Stroke));
    public static string BaseTimes_Header_BaseTime => Get(nameof(BaseTimes_Header_BaseTime));
    public static string BaseTimes_Header_BaseTimeSeconds => Get(nameof(BaseTimes_Header_BaseTimeSeconds));

    public static string Templates_EntryDoc_GroupHeader => Get(nameof(Templates_EntryDoc_GroupHeader));
    public static string Templates_EntryDoc_Title => Get(nameof(Templates_EntryDoc_Title));
    public static string Templates_EntryDoc_Subtitle => Get(nameof(Templates_EntryDoc_Subtitle));
    public static string Templates_Download => Get(nameof(Templates_Download));
    public static string Templates_Download_Tooltip => Get(nameof(Templates_Download_Tooltip));

    public static string Dialog_SaveExcelTemplate_Title => Get(nameof(Dialog_SaveExcelTemplate_Title));
    public static string Dialog_SaveExcelTemplate_Filter => Get(nameof(Dialog_SaveExcelTemplate_Filter));
    public static string Dialog_SaveExcelTemplate_DefaultFileName => Get(nameof(Dialog_SaveExcelTemplate_DefaultFileName));

    public static string Language_Label => Get(nameof(Language_Label));
    public static string Language_English => Get(nameof(Language_English));
    public static string Language_Russian => Get(nameof(Language_Russian));

    public static string CompetitionSelection_Subtitle => Get(nameof(CompetitionSelection_Subtitle));
    public static string CompetitionSelection_Create => Get(nameof(CompetitionSelection_Create));
    public static string CompetitionSelection_Open => Get(nameof(CompetitionSelection_Open));

    public static string Toolbar_Create => Get(nameof(Toolbar_Create));
    public static string Toolbar_Edit => Get(nameof(Toolbar_Edit));
    public static string Toolbar_Delete => Get(nameof(Toolbar_Delete));
    public static string Toolbar_Refresh => Get(nameof(Toolbar_Refresh));
    public static string Toolbar_ToggleFilters => Get(nameof(Toolbar_ToggleFilters));
    public static string Paging_First => Get(nameof(Paging_First));
    public static string Paging_Previous => Get(nameof(Paging_Previous));
    public static string Paging_Next => Get(nameof(Paging_Next));
    public static string Paging_Last => Get(nameof(Paging_Last));
    public static string Paging_ItemsInfo_EmptyFormat => Get(nameof(Paging_ItemsInfo_EmptyFormat));
    public static string Paging_ItemsInfo_RangeFormat => Get(nameof(Paging_ItemsInfo_RangeFormat));

    public static string Import_Details => Get(nameof(Import_Details));
    public static string Import_Cancel => Get(nameof(Import_Cancel));
    public static string Import_Close => Get(nameof(Import_Close));
    public static string Import_WarningsFormat => Get(nameof(Import_WarningsFormat));
    public static string Import_ErrorsFormat => Get(nameof(Import_ErrorsFormat));
    public static string Import_Col_File => Get(nameof(Import_Col_File));
    public static string Import_Col_Path => Get(nameof(Import_Col_Path));
    public static string Import_Col_Status => Get(nameof(Import_Col_Status));
    public static string Import_Col_Clubs => Get(nameof(Import_Col_Clubs));
    public static string Import_Col_Athletes => Get(nameof(Import_Col_Athletes));
    public static string Import_Col_Entries => Get(nameof(Import_Col_Entries));
    public static string Import_Col_Warnings => Get(nameof(Import_Col_Warnings));
    public static string Import_Col_Errors => Get(nameof(Import_Col_Errors));
    public static string Import_EventPrev => Get(nameof(Import_EventPrev));
    public static string Import_EventCurrent => Get(nameof(Import_EventCurrent));
    public static string Import_CreatedEntries => Get(nameof(Import_CreatedEntries));

    public static string Filters_Clear => Get(nameof(Filters_Clear));
    public static string Filters_Round => Get(nameof(Filters_Round));
    public static string Filters_Distance => Get(nameof(Filters_Distance));
    public static string Filters_Stroke => Get(nameof(Filters_Stroke));
    public static string Filters_Gender => Get(nameof(Filters_Gender));
    public static string Filters_Status => Get(nameof(Filters_Status));
    public static string Entries_ImportFromFile => Get(nameof(Entries_ImportFromFile));
    public static string Entries_ImportFromPrevEvent => Get(nameof(Entries_ImportFromPrevEvent));

    public static string Entries_Col_Distance => Get(nameof(Entries_Col_Distance));
    public static string Entries_Col_Participant => Get(nameof(Entries_Col_Participant));
    public static string Entries_Col_Team => Get(nameof(Entries_Col_Team));
    public static string Entries_Col_Status => Get(nameof(Entries_Col_Status));
    public static string Entries_Col_EntryTime => Get(nameof(Entries_Col_EntryTime));
    public static string Entries_Col_FinishTime => Get(nameof(Entries_Col_FinishTime));
    public static string Entries_Col_Points => Get(nameof(Entries_Col_Points));
    public static string Entries_Col_Comment => Get(nameof(Entries_Col_Comment));
    public static string Distance_MetersFormat => Get(nameof(Distance_MetersFormat));

    public static string Import_Summary_Total => Get(nameof(Import_Summary_Total));
    public static string Import_Summary_FilesCountFormat => Get(nameof(Import_Summary_FilesCountFormat));

    public static string Import_Event_Success_Header => Get(nameof(Import_Event_Success_Header));
    public static string Import_Event_Success_MessageFormat => Get(nameof(Import_Event_Success_MessageFormat));
    public static string Import_Event_Partial_Header => Get(nameof(Import_Event_Partial_Header));
    public static string Import_Event_Partial_MessageFormat => Get(nameof(Import_Event_Partial_MessageFormat));
    public static string Import_Event_Failed_Header => Get(nameof(Import_Event_Failed_Header));
    public static string Import_Event_Failed_MessageFormat => Get(nameof(Import_Event_Failed_MessageFormat));
    public static string Import_Event_Error_Header => Get(nameof(Import_Event_Error_Header));

    public static string Dialog_SelectEntryFiles_Title => Get(nameof(Dialog_SelectEntryFiles_Title));
    public static string Dialog_SelectEntryFiles_Filter => Get(nameof(Dialog_SelectEntryFiles_Filter));
    public static string Dialog_SaveExcelReports_Filter => Get(nameof(Dialog_SaveExcelReports_Filter));
    public static string Dialog_SaveExcelReports_DefaultExt => Get(nameof(Dialog_SaveExcelReports_DefaultExt));
    public static string Dialog_SaveExcelReports_DefaultFileName => Get(nameof(Dialog_SaveExcelReports_DefaultFileName));

    public static string Dialog_CompetitionDb_Filter => Get(nameof(Dialog_CompetitionDb_Filter));
    public static string Dialog_CompetitionDb_DefaultExt => Get(nameof(Dialog_CompetitionDb_DefaultExt));

    public static string Import_File_Header => Get(nameof(Import_File_Header));
    public static string Import_File_Preparing_MessageFormat => Get(nameof(Import_File_Preparing_MessageFormat));
    public static string Import_File_Processing_MessageFormat => Get(nameof(Import_File_Processing_MessageFormat));
    public static string Import_File_Canceled_Header => Get(nameof(Import_File_Canceled_Header));
    public static string Import_File_Finished_MessageFormat => Get(nameof(Import_File_Finished_MessageFormat));

    public static string Dialog_Error_SaveReport_Title => Get(nameof(Dialog_Error_SaveReport_Title));
    public static string Dialog_Error_FileBusyOrUnavailable => Get(nameof(Dialog_Error_FileBusyOrUnavailable));
    public static string Dialog_Error_FileBusyOrUnavailableWithDetailsFormat => Get(nameof(Dialog_Error_FileBusyOrUnavailableWithDetailsFormat));
    public static string Dialog_Error_SaveBaseTimes_Title => Get(nameof(Dialog_Error_SaveBaseTimes_Title));
    public static string Dialog_Error_BaseTimesFileBusyOrUnavailable => Get(nameof(Dialog_Error_BaseTimesFileBusyOrUnavailable));
    public static string Dialog_Error_SaveFile_Title => Get(nameof(Dialog_Error_SaveFile_Title));

    public static string Dialog_CreateCompetition_Title => Get(nameof(Dialog_CreateCompetition_Title));
    public static string Dialog_OpenCompetition_Title => Get(nameof(Dialog_OpenCompetition_Title));
    public static string Dialog_Error_CreateDbFile_Title => Get(nameof(Dialog_Error_CreateDbFile_Title));

    public static string Reports_WindowTitle => Get(nameof(Reports_WindowTitle));
    public static string Reports_SaveDialog_Title => Get(nameof(Reports_SaveDialog_Title));
    public static string Reports_Validation_SelectAtLeastOne => Get(nameof(Reports_Validation_SelectAtLeastOne));
    public static string Reports_Validation_SelectOutputFile => Get(nameof(Reports_Validation_SelectOutputFile));

    public static string LoadPrev_WindowTitle => Get(nameof(LoadPrev_WindowTitle));
    public static string LoadPrev_Validation_SelectOfficialPreviousEvent => Get(nameof(LoadPrev_Validation_SelectOfficialPreviousEvent));
    public static string LoadPrev_Validation_SelectCurrentEvent => Get(nameof(LoadPrev_Validation_SelectCurrentEvent));
    public static string LoadPrev_Validation_EventsMustDiffer => Get(nameof(LoadPrev_Validation_EventsMustDiffer));
    public static string LoadPrev_Validation_RoundParticipantsRequired => Get(nameof(LoadPrev_Validation_RoundParticipantsRequired));

    public static string Events_Col_Order => Get(nameof(Events_Col_Order));
    public static string Events_Col_Date => Get(nameof(Events_Col_Date));
    public static string Events_Col_Time => Get(nameof(Events_Col_Time));
    public static string Events_Col_Round => Get(nameof(Events_Col_Round));
    public static string Events_Col_Distance => Get(nameof(Events_Col_Distance));
    public static string Events_Col_AgeGroup => Get(nameof(Events_Col_AgeGroup));
    public static string Events_Col_Lanes => Get(nameof(Events_Col_Lanes));
    public static string Events_Col_Status => Get(nameof(Events_Col_Status));

    public static string WindowMode_Create => Get(nameof(WindowMode_Create));
    public static string WindowMode_Edit => Get(nameof(WindowMode_Edit));
    public static string Validation_ErrorFallback => Get(nameof(Validation_ErrorFallback));

    public static string StartTimes_WindowTitle => Get(nameof(StartTimes_WindowTitle));
    public static string StartTimes_Validation_StartTimeRequired => Get(nameof(StartTimes_Validation_StartTimeRequired));
    public static string StartTimes_Validation_HeatPauseNonNegative => Get(nameof(StartTimes_Validation_HeatPauseNonNegative));
    public static string StartTimes_Validation_EventPauseNonNegative => Get(nameof(StartTimes_Validation_EventPauseNonNegative));

    public static string Heats_DeleteTooltip_Heat => Get(nameof(Heats_DeleteTooltip_Heat));
    public static string Heats_DeleteTooltip_Position => Get(nameof(Heats_DeleteTooltip_Position));

    public static string Fixation_SelectedHeatHeader_Format => Get(nameof(Fixation_SelectedHeatHeader_Format));

    public static string WindowTitle_CreateClub => Get(nameof(WindowTitle_CreateClub));
    public static string WindowTitle_EditClub => Get(nameof(WindowTitle_EditClub));
    public static string WindowTitle_CreateAthlete => Get(nameof(WindowTitle_CreateAthlete));
    public static string WindowTitle_EditAthlete => Get(nameof(WindowTitle_EditAthlete));
    public static string WindowTitle_CreateEvent => Get(nameof(WindowTitle_CreateEvent));
    public static string WindowTitle_EditEvent => Get(nameof(WindowTitle_EditEvent));
    public static string WindowTitle_CreateEntry => Get(nameof(WindowTitle_CreateEntry));
    public static string WindowTitle_EditEntry => Get(nameof(WindowTitle_EditEntry));
    public static string WindowTitle_CreateHeat => Get(nameof(WindowTitle_CreateHeat));
    public static string WindowTitle_EditHeat => Get(nameof(WindowTitle_EditHeat));
    public static string WindowTitle_CreateSwimStyle => Get(nameof(WindowTitle_CreateSwimStyle));
    public static string WindowTitle_EditSwimStyle => Get(nameof(WindowTitle_EditSwimStyle));
    public static string WindowTitle_CreateAgeGroup => Get(nameof(WindowTitle_CreateAgeGroup));
    public static string WindowTitle_EditAgeGroup => Get(nameof(WindowTitle_EditAgeGroup));

    public static string Common_NoneParen => Get(nameof(Common_NoneParen));
    public static string Common_PersonalParen => Get(nameof(Common_PersonalParen));

    public static string HeatAlloc_WindowTitle => Get(nameof(HeatAlloc_WindowTitle));
    public static string HeatAlloc_Validation_MinHeatSize => Get(nameof(HeatAlloc_Validation_MinHeatSize));

    public static string Heat_Validation_EventRequired => Get(nameof(Heat_Validation_EventRequired));
    public static string Heat_Validation_MinHeatNumber => Get(nameof(Heat_Validation_MinHeatNumber));
    public static string Heat_Validation_MaxPositionsFormat => Get(nameof(Heat_Validation_MaxPositionsFormat));

    public static string Clubs_Col_Name => Get(nameof(Clubs_Col_Name));
    public static string Clubs_Col_Athletes => Get(nameof(Clubs_Col_Athletes));
    public static string Clubs_Col_Entries => Get(nameof(Clubs_Col_Entries));
    public static string Clubs_Col_Relays => Get(nameof(Clubs_Col_Relays));
    public static string Clubs_Col_Points => Get(nameof(Clubs_Col_Points));

    public static string Athletes_Col_FirstName => Get(nameof(Athletes_Col_FirstName));
    public static string Athletes_Col_LastName => Get(nameof(Athletes_Col_LastName));
    public static string Athletes_Col_Gender => Get(nameof(Athletes_Col_Gender));
    public static string Athletes_Col_BirthYear => Get(nameof(Athletes_Col_BirthYear));
    public static string Athletes_Col_Category => Get(nameof(Athletes_Col_Category));
    public static string Athletes_Col_Team => Get(nameof(Athletes_Col_Team));

    public static string SwimStyles_Col_Name => Get(nameof(SwimStyles_Col_Name));
    public static string SwimStyles_Col_Distance => Get(nameof(SwimStyles_Col_Distance));
    public static string SwimStyles_Col_Stroke => Get(nameof(SwimStyles_Col_Stroke));

    public static string AgeGroups_Col_Name => Get(nameof(AgeGroups_Col_Name));
    public static string AgeGroups_Col_Gender => Get(nameof(AgeGroups_Col_Gender));
    public static string AgeGroups_Col_BirthYearFrom => Get(nameof(AgeGroups_Col_BirthYearFrom));
    public static string AgeGroups_Col_BirthYearTo => Get(nameof(AgeGroups_Col_BirthYearTo));

    public static string Heats_GroupHeader_Format => Get(nameof(Heats_GroupHeader_Format));

    public static string Enum_HeatStatus_NOT_STARTED => Get(nameof(Enum_HeatStatus_NOT_STARTED));
    public static string Enum_HeatStatus_UNOFFICIAL => Get(nameof(Enum_HeatStatus_UNOFFICIAL));
    public static string Enum_HeatStatus_OFFICIAL => Get(nameof(Enum_HeatStatus_OFFICIAL));
    public static string Enum_SwimEventStatus_EMPTY => Get(nameof(Enum_SwimEventStatus_EMPTY));
    public static string Enum_SwimEventStatus_ENTRY => Get(nameof(Enum_SwimEventStatus_ENTRY));
    public static string Enum_SwimEventStatus_NOT_STARTED => Get(nameof(Enum_SwimEventStatus_NOT_STARTED));
    public static string Enum_SwimEventStatus_RUNNING => Get(nameof(Enum_SwimEventStatus_RUNNING));
    public static string Enum_SwimEventStatus_OFFICIAL => Get(nameof(Enum_SwimEventStatus_OFFICIAL));
    public static string Enum_EventRound_PRE => Get(nameof(Enum_EventRound_PRE));
    public static string Enum_EventRound_SOP => Get(nameof(Enum_EventRound_SOP));
    public static string Enum_EventRound_SEM => Get(nameof(Enum_EventRound_SEM));
    public static string Enum_EventRound_SOS => Get(nameof(Enum_EventRound_SOS));
    public static string Enum_EventRound_FIN => Get(nameof(Enum_EventRound_FIN));
    public static string Enum_EntryStatus_ENTRY => Get(nameof(Enum_EntryStatus_ENTRY));
    public static string Enum_EntryStatus_EVENT => Get(nameof(Enum_EntryStatus_EVENT));
    public static string Enum_EntryStatus_HEAT => Get(nameof(Enum_EntryStatus_HEAT));
    public static string Enum_EntryStatus_FINISH => Get(nameof(Enum_EntryStatus_FINISH));
    public static string Enum_EntryStatus_DSQ => Get(nameof(Enum_EntryStatus_DSQ));
    public static string Enum_EntryStatus_DNS => Get(nameof(Enum_EntryStatus_DNS));
    public static string Enum_EntryStatus_DNF => Get(nameof(Enum_EntryStatus_DNF));
    public static string Enum_ImportFileStatus_Summary => Get(nameof(Enum_ImportFileStatus_Summary));

    public static string Common_Cancel => Get(nameof(Common_Cancel));
    public static string Common_Save => Get(nameof(Common_Save));
    public static string Common_Add => Get(nameof(Common_Add));
    public static string Common_Auto => Get(nameof(Common_Auto));
    public static string Common_SearchPlaceholder => Get(nameof(Common_SearchPlaceholder));
    public static string Common_Men => Get(nameof(Common_Men));
    public static string Common_Women => Get(nameof(Common_Women));
    public static string Common_Mixed => Get(nameof(Common_Mixed));
    public static string Common_All => Get(nameof(Common_All));
    public static string Common_Min => Get(nameof(Common_Min));
    public static string Common_Max => Get(nameof(Common_Max));
    public static string Common_AbsoluteMin => Get(nameof(Common_AbsoluteMin));
    public static string Common_AbsoluteMax => Get(nameof(Common_AbsoluteMax));
    public static string Common_Individual => Get(nameof(Common_Individual));
    public static string Common_NoTime => Get(nameof(Common_NoTime));
    public static string Common_Ok => Get(nameof(Common_Ok));

    public static string Window_Minimize => Get(nameof(Window_Minimize));
    public static string Window_Maximize => Get(nameof(Window_Maximize));
    public static string Window_Restore => Get(nameof(Window_Restore));
    public static string Window_Close => Get(nameof(Window_Close));

    public static string App_Title => Get(nameof(App_Title));

    public static string Tabs_Athletes => Get(nameof(Tabs_Athletes));
    public static string Tabs_Entries => Get(nameof(Tabs_Entries));
    public static string Tabs_Heats => Get(nameof(Tabs_Heats));
    public static string Tabs_Results => Get(nameof(Tabs_Results));
    public static string Tabs_Fixation => Get(nameof(Tabs_Fixation));
    public static string Tabs_Heat => Get(nameof(Tabs_Heat));
    public static string Tabs_Events => Get(nameof(Tabs_Events));

    public static string Field_Name => Get(nameof(Field_Name));

    public static string Heat_Field_Number => Get(nameof(Heat_Field_Number));
    public static string Heat_Field_StartTime => Get(nameof(Heat_Field_StartTime));
    public static string Heat_Field_Status => Get(nameof(Heat_Field_Status));
    public static string Heat_Col_Lane => Get(nameof(Heat_Col_Lane));
    public static string Heat_Col_Entry => Get(nameof(Heat_Col_Entry));

    public static string StartTimes_Field_StartTime => Get(nameof(StartTimes_Field_StartTime));
    public static string StartTimes_Field_HeatPauseMinutes => Get(nameof(StartTimes_Field_HeatPauseMinutes));
    public static string StartTimes_Field_EventPauseMinutes => Get(nameof(StartTimes_Field_EventPauseMinutes));

    public static string Event_Field_Order => Get(nameof(Event_Field_Order));
    public static string Event_Field_Date => Get(nameof(Event_Field_Date));
    public static string Event_Field_Time => Get(nameof(Event_Field_Time));
    public static string Event_Field_SwimStyle => Get(nameof(Event_Field_SwimStyle));
    public static string Event_Field_AgeGroup => Get(nameof(Event_Field_AgeGroup));
    public static string Event_Field_Course => Get(nameof(Event_Field_Course));
    public static string Event_Field_Round => Get(nameof(Event_Field_Round));
    public static string Event_Field_PreviousEvent => Get(nameof(Event_Field_PreviousEvent));
    public static string Event_Field_RoundParticipantsCount => Get(nameof(Event_Field_RoundParticipantsCount));
    public static string Event_Field_AvailableLanes => Get(nameof(Event_Field_AvailableLanes));
    public static string Event_Lanes_Tab_Range => Get(nameof(Event_Lanes_Tab_Range));
    public static string Event_Lanes_Tab_Custom => Get(nameof(Event_Lanes_Tab_Custom));
    public static string Event_Validation_CustomLaneNamesRequired => Get(nameof(Event_Validation_CustomLaneNamesRequired));
    public static string Event_Field_CustomLaneNames => Get(nameof(Event_Field_CustomLaneNames));
    public static string Event_Field_CustomLaneNames_Placeholder => Get(nameof(Event_Field_CustomLaneNames_Placeholder));

    public static string LoadPrev_Field_PreviousEvent => Get(nameof(LoadPrev_Field_PreviousEvent));
    public static string LoadPrev_Field_CurrentEvent => Get(nameof(LoadPrev_Field_CurrentEvent));

    public static string Entry_Tab_Individual => Get(nameof(Entry_Tab_Individual));
    public static string Entry_Tab_Relay => Get(nameof(Entry_Tab_Relay));
    public static string Entry_Field_Athlete => Get(nameof(Entry_Field_Athlete));
    public static string Entry_Field_Event => Get(nameof(Entry_Field_Event));
    public static string Entry_Field_EntryTime => Get(nameof(Entry_Field_EntryTime));
    public static string Entry_Field_Scoring => Get(nameof(Entry_Field_Scoring));
    public static string Entry_Field_Club => Get(nameof(Entry_Field_Club));
    public static string Entry_Field_RelayNumber => Get(nameof(Entry_Field_RelayNumber));

    public static string Reports_Include_EntryList => Get(nameof(Reports_Include_EntryList));
    public static string Reports_Include_StartList => Get(nameof(Reports_Include_StartList));
    public static string Reports_Include_FinishList => Get(nameof(Reports_Include_FinishList));
    public static string Reports_Field_OutputFile => Get(nameof(Reports_Field_OutputFile));

    public static string HeatAlloc_Field_Order => Get(nameof(HeatAlloc_Field_Order));
    public static string HeatAlloc_Order_WeakToStrong => Get(nameof(HeatAlloc_Order_WeakToStrong));
    public static string HeatAlloc_Order_StrongToWeak => Get(nameof(HeatAlloc_Order_StrongToWeak));
    public static string HeatAlloc_Field_MinHeatSize => Get(nameof(HeatAlloc_Field_MinHeatSize));

    public static string Common_Delete => Get(nameof(Common_Delete));
    public static string Common_Allocate => Get(nameof(Common_Allocate));
    public static string Common_Reform => Get(nameof(Common_Reform));

    public static string Confirm_Title_Delete => Get(nameof(Confirm_Title_Delete));
    public static string Confirm_Title_HeatAllocation => Get(nameof(Confirm_Title_HeatAllocation));

    public static string Common_Entry_Accusative_Singular => Get(nameof(Common_Entry_Accusative_Singular));
    public static string Common_Entry_Accusative_Few => Get(nameof(Common_Entry_Accusative_Few));
    public static string Common_Entry_Accusative_Many => Get(nameof(Common_Entry_Accusative_Many));

    public static string Confirm_DeleteOfficialResults_MessageFormat => Get(nameof(Confirm_DeleteOfficialResults_MessageFormat));
    public static string Confirm_HeatAllocationOfficialResults_MessageFormat => Get(nameof(Confirm_HeatAllocationOfficialResults_MessageFormat));

    public static string SwimStyle_Field_Stroke => Get(nameof(SwimStyle_Field_Stroke));
    public static string SwimStyle_Field_Distance => Get(nameof(SwimStyle_Field_Distance));
    public static string SwimStyle_Field_RelayCount => Get(nameof(SwimStyle_Field_RelayCount));

    public static string Athlete_Field_FirstName => Get(nameof(Athlete_Field_FirstName));
    public static string Athlete_Field_LastName => Get(nameof(Athlete_Field_LastName));
    public static string Athlete_Field_Gender => Get(nameof(Athlete_Field_Gender));
    public static string Athlete_Field_BirthYear => Get(nameof(Athlete_Field_BirthYear));
    public static string Athlete_Field_Category => Get(nameof(Athlete_Field_Category));
    public static string Athlete_Field_Club => Get(nameof(Athlete_Field_Club));

    public static string Events_AllocateHeats => Get(nameof(Events_AllocateHeats));
    public static string Events_GenerateReports => Get(nameof(Events_GenerateReports));
    public static string Events_CalculateStartTimes => Get(nameof(Events_CalculateStartTimes));

    public static string Heats_Create => Get(nameof(Heats_Create));
    public static string Heats_Edit => Get(nameof(Heats_Edit));
    public static string Heats_Refresh => Get(nameof(Heats_Refresh));
    public static string Heats_Col_Lane => Get(nameof(Heats_Col_Lane));
    public static string Heats_Col_Participant => Get(nameof(Heats_Col_Participant));
    public static string Heats_Col_BirthYear => Get(nameof(Heats_Col_BirthYear));
    public static string Heats_Col_Team => Get(nameof(Heats_Col_Team));
    public static string Heats_Col_EntryTime => Get(nameof(Heats_Col_EntryTime));
    public static string Heats_Col_Result => Get(nameof(Heats_Col_Result));

    public static string Results_Col_Event => Get(nameof(Results_Col_Event));
    public static string Results_Col_Participant => Get(nameof(Results_Col_Participant));
    public static string Results_Col_Team => Get(nameof(Results_Col_Team));
    public static string Results_Col_Place => Get(nameof(Results_Col_Place));
    public static string Results_Col_Result => Get(nameof(Results_Col_Result));
    public static string Results_Col_Points => Get(nameof(Results_Col_Points));

    public static string Fixation_Refresh => Get(nameof(Fixation_Refresh));
    public static string Fixation_Col_Lane => Get(nameof(Fixation_Col_Lane));
    public static string Fixation_Col_Participant => Get(nameof(Fixation_Col_Participant));
    public static string Fixation_Col_BirthYear => Get(nameof(Fixation_Col_BirthYear));
    public static string Fixation_Col_Club => Get(nameof(Fixation_Col_Club));
    public static string Fixation_Col_EntryTime => Get(nameof(Fixation_Col_EntryTime));
    public static string Fixation_Col_Status => Get(nameof(Fixation_Col_Status));
    public static string Fixation_Col_Result => Get(nameof(Fixation_Col_Result));
    public static string Fixation_Col_Points => Get(nameof(Fixation_Col_Points));
    public static string Fixation_Col_Comment => Get(nameof(Fixation_Col_Comment));
    public static string Fixation_Comment_Tooltip => Get(nameof(Fixation_Comment_Tooltip));
}

