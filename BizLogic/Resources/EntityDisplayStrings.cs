using System.Globalization;
using System.Resources;

namespace BizLogic.Resources;

public static class EntityDisplayStrings
{
    private static readonly ResourceManager ResourceManagerImpl =
        new("BizLogic.Resources.EntityDisplayStrings", typeof(EntityDisplayStrings).Assembly);

    public static ResourceManager ResourceManager => ResourceManagerImpl;

    public static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? $"[[{name}]]";

    public static string SwimStyle_DisplayName_Format => Get(nameof(SwimStyle_DisplayName_Format));
    public static string SwimEvent_DisplayName_Format => Get(nameof(SwimEvent_DisplayName_Format));
    public static string AgeGroup_DisplayName_Format => Get(nameof(AgeGroup_DisplayName_Format));
    public static string AgeGroup_YearRange_Open => Get(nameof(AgeGroup_YearRange_Open));
    public static string AgeGroup_YearRange_OpenWithSuffix => Get(nameof(AgeGroup_YearRange_OpenWithSuffix));
    public static string AgeGroup_YearRange_WithSuffix_Format => Get(nameof(AgeGroup_YearRange_WithSuffix_Format));
    public static string AgeGroup_YearRange_OlderThan => Get(nameof(AgeGroup_YearRange_OlderThan));
    public static string AgeGroup_YearRange_YoungerThan => Get(nameof(AgeGroup_YearRange_YoungerThan));
}
