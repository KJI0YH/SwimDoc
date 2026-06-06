using System.Globalization;
using System.Resources;

namespace BizLogic.Resources;

public static class ReportExcelStrings
{
    private static readonly ResourceManager ResourceManagerImpl =
        new("BizLogic.Resources.ReportExcelStrings", typeof(ReportExcelStrings).Assembly);

    public static ResourceManager ResourceManager => ResourceManagerImpl;

    public static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? $"[[{name}]]";

    public static string Sheet_EntryList => Get(nameof(Sheet_EntryList));
    public static string Sheet_StartList => Get(nameof(Sheet_StartList));
    public static string Sheet_FinishList => Get(nameof(Sheet_FinishList));
    public static string Sheet_CombinedResults => Get(nameof(Sheet_CombinedResults));

    public static string Col_No => Get(nameof(Col_No));
    public static string Col_Lane => Get(nameof(Col_Lane));
    public static string Col_Participant => Get(nameof(Col_Participant));
    public static string Col_BirthYear => Get(nameof(Col_BirthYear));
    public static string Col_Team => Get(nameof(Col_Team));
    public static string Col_Time => Get(nameof(Col_Time));
    public static string Col_Points => Get(nameof(Col_Points));
    public static string Col_Total => Get(nameof(Col_Total));
    public static string Col_Comment => Get(nameof(Col_Comment));

    public static string Value_NoneParen => Get(nameof(Value_NoneParen));
    public static string Value_PersonalParen => Get(nameof(Value_PersonalParen));

    public static string HeatTitle_Format => Get(nameof(HeatTitle_Format));
}

