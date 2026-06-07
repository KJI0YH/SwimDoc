using System.Globalization;
using System.Resources;

namespace BizLogic.Resources;

public static class EntryImportStrings
{
    private static readonly ResourceManager ResourceManagerImpl =
        new("BizLogic.Resources.EntryImportStrings", typeof(EntryImportStrings).Assembly);

    public static ResourceManager ResourceManager => ResourceManagerImpl;
    public static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? $"[[{name}]]";

    public static string FileNotFound_Format => Get(nameof(FileNotFound_Format));
    public static string FileBusyOrUnavailable_Format => Get(nameof(FileBusyOrUnavailable_Format));
    public static string HeaderNotFound_Format => Get(nameof(HeaderNotFound_Format));
    public static string ClubNameNotFound_PersonalScoring_Format => Get(nameof(ClubNameNotFound_PersonalScoring_Format));
    public static string DistanceParseFailed_Format => Get(nameof(DistanceParseFailed_Format));
    public static string StrokeParseFailed_Format => Get(nameof(StrokeParseFailed_Format));
    public static string AthleteFirstNameInvalid_Format => Get(nameof(AthleteFirstNameInvalid_Format));
    public static string AthleteLastNameInvalid_Format => Get(nameof(AthleteLastNameInvalid_Format));
    public static string AthleteBirthYearInvalid_Format => Get(nameof(AthleteBirthYearInvalid_Format));
    public static string AthleteGenderInvalid_Format => Get(nameof(AthleteGenderInvalid_Format));
    public static string AthleteCategoryInvalid_Format => Get(nameof(AthleteCategoryInvalid_Format));
}
