using DataLayer.EfClasses;
using OfficeOpenXml;
using System.Drawing;

namespace BizLogic.ReportGenerator.Concrete.Excel;

internal static class ReportExcelScoringHelper
{
    public static readonly Color NonScoringFillColor = Color.FromArgb(0xD3, 0xD3, 0xD3);
    public static bool IsNonScoringEntry(Entry entry) => !entry.Scoring;
    public static bool IsNonScoringSwimEvent(SwimEvent swimEvent)
    {
        var entries = GetSwimEventEntries(swimEvent).ToList();
        return entries.Count > 0 && entries.All(entry => !entry.Scoring);
    }

    public static IEnumerable<Entry> GetSwimEventEntries(SwimEvent swimEvent)
    {
        if (swimEvent.Entries is { Count: > 0 })
            return swimEvent.Entries;
        if (swimEvent.Heats is null)
            return [];
        return swimEvent.Heats
            .Where(heat => heat.Positions is not null)
            .SelectMany(heat => heat.Positions)
            .Select(position => position.Entry)
            .OfType<Entry>();
    }

    public static void ApplyNonScoringFill(ExcelRange range) =>
        range.Style.Fill.SetBackground(NonScoringFillColor);
}
