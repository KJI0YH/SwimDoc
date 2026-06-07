using System.ComponentModel;
using System.Reflection;
using DataLayer.EfClasses;

namespace BizLogic.EntryDocumentReader;

internal static class EntryDocumentEnumParser
{
    private static readonly IReadOnlyDictionary<string, Category> CategoryByLabel = BuildEnumLabelMap(
        new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase)
        {
            ["I jr."] = Category.FirstJunior,
            ["II jr."] = Category.SecondJunior
        });

    private static readonly IReadOnlyDictionary<string, Gender> GenderByLabel = BuildEnumLabelMap(
        new Dictionary<string, Gender>(StringComparer.OrdinalIgnoreCase)
        {
            ["М"] = Gender.Male,
            ["M"] = Gender.Male,
            ["Муж"] = Gender.Male,
            ["Male"] = Gender.Male,
            ["Men"] = Gender.Male,
            ["Ж"] = Gender.Female,
            ["W"] = Gender.Female,
            ["F"] = Gender.Female,
            ["Жен"] = Gender.Female,
            ["Female"] = Gender.Female,
            ["Women"] = Gender.Female
        });

    private static readonly IReadOnlyDictionary<string, Stroke> StrokeByLabel = BuildEnumLabelMap(
        new Dictionary<string, Stroke>(StringComparer.OrdinalIgnoreCase)
        {
            ["Butterfly"] = Stroke.Fly,
            ["Backstroke"] = Stroke.Back,
            ["Breaststroke"] = Stroke.Breast,
            ["Freestyle"] = Stroke.Free,
            ["Medley"] = Stroke.Medley
        });

    public static bool TryParseCategory(string? text, out Category category) =>
        TryParse(text, CategoryByLabel, out category);

    public static bool TryParseGender(string? text, out Gender gender) =>
        TryParse(text, GenderByLabel, out gender);

    public static bool TryParseStroke(string? text, out Stroke stroke) =>
        TryParse(text, StrokeByLabel, out stroke);

    private static bool TryParse<T>(string? text, IReadOnlyDictionary<string, T> labels, out T value)
        where T : struct
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return labels.TryGetValue(text.Trim(), out value);
    }

    private static Dictionary<string, TEnum> BuildEnumLabelMap<TEnum>(Dictionary<string, TEnum> extras)
        where TEnum : struct, Enum
    {
        foreach (var value in Enum.GetValues<TEnum>())
        {
            extras.TryAdd(value.ToString(), value);
            var field = typeof(TEnum).GetField(value.ToString());
            var description = field?.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrWhiteSpace(description))
                extras.TryAdd(description, value);
        }

        return extras;
    }
}
