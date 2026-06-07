using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class EntryTimeDisplay
{
    public const string NotEnteredText = "N.T.";
    public static string FormatEntryTime(int? entryTime) =>
        entryTime == null ? NotEnteredText : FormatHundredths(entryTime.Value);

    public static string FormatFinishTime(Entry entry) =>
        entry.Status switch
        {
            EntryStatus.FINISH => entry.FinishTime.HasValue
                ? FormatHundredths(entry.FinishTime.Value)
                : string.Empty,
            EntryStatus.DNF or EntryStatus.DNS or EntryStatus.DSQ => entry.Status.ToString(),
            _ => string.Empty
        };

    public static string FormatHundredths(int totalHundredths)
    {
        if (totalHundredths < 0)
            totalHundredths = 0;
        var minutes = totalHundredths / 6000;
        var seconds = totalHundredths % 6000 / 100;
        var hundredths = totalHundredths % 100;
        return minutes > 0
            ? $"{minutes}:{seconds:D2}.{hundredths:D2}"
            : $"{seconds}.{hundredths:D2}";
    }
}
