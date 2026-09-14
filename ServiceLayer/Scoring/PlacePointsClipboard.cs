using System.Globalization;

namespace ServiceLayer.Scoring;

public static class PlacePointsClipboard
{
    public static IReadOnlyList<int> ParseColumn(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var result = new List<int>();
        foreach (var line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var cell = line.Split('\t')[0].Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(cell))
                continue;
            if (int.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
                int.TryParse(cell, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
            {
                result.Add(Math.Max(0, value));
            }
        }

        return result;
    }
}
