using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class SwimStyleDisplay
{
    public static string FormatBasic(SwimStyle style) =>
        $"{(style.IsRelay ? style.RelayCount + "x" : string.Empty)}{style.Distance}м {EnumDisplay.GetDescription(style.Stroke)}";
}
