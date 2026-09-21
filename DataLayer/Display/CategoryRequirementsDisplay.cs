using System.Globalization;
using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class CategoryRequirementsDisplay
{
    public static string FormatLine(IReadOnlyDictionary<Category, int> norms, CultureInfo? culture = null)
    {
        var parts = new List<string>();
        foreach (var category in CategoryDisplay.FromHighest)
        {
            if (!norms.TryGetValue(category, out var time) || time <= 0)
                continue;
            parts.Add($"{CategoryDisplay.Format(category, culture)}: {EntryTimeDisplay.FormatHundredths(time)}");
        }

        return parts.Count == 0
            ? string.Empty
            : string.Join(" / ", parts);
    }
}
