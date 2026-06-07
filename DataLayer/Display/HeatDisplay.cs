using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class HeatDisplay
{
    public static string FormatDayTime(TimeOnly? dayTime) => StartTimeDisplay.Format(dayTime);
    public static string FormatNumberWithTime(Heat heat) =>
        StartTimeDisplay.IsSet(heat.DayTime)
            ? $"{heat.Number} ({FormatDayTime(heat.DayTime)})"
            : heat.Number.ToString();
}
