using System.Globalization;
using DataLayer.Display;

namespace UI.Helpers.Input;

public static class SwimTimeInput
{
    public static string Format(int? hundredths) =>
        hundredths.HasValue ? EntryTimeDisplay.FormatHundredths(hundredths.Value) : string.Empty;

    public static string ExtractDigits(string? text) =>
        new string((text ?? string.Empty).Where(char.IsDigit).ToArray());

    public static int? ParseDigits(string? text) =>
        ParseDigitsFromDigitsOnly(ExtractDigits(text));

    public static SwimTimeTextChange ApplyText(string? text)
    {
        var hundredths = ParseDigits(text);
        return new SwimTimeTextChange(Format(hundredths), hundredths);
    }

    public static string FormatSecondsField(int? hundredths)
    {
        if (!hundredths.HasValue)
            return string.Empty;
        var value = hundredths.Value;
        if (value <= 0)
            return string.Empty;
        return (value / 100d).ToString("0.00", CultureInfo.InvariantCulture);
    }

    public static int? ParseSecondsField(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var digits = ExtractDigits(text);
        if (digits.Length == 0)
            return null;
        var normalized = digits.TrimStart('0');
        if (normalized.Length == 0)
            return null;
        if (normalized.Length > 9)
            normalized = normalized[^9..];
        normalized = normalized.PadLeft(3, '0');
        var hundredthsPart = normalized[^2..];
        var secondsPart = normalized[..^2];
        if (!int.TryParse(secondsPart, out var seconds))
            seconds = 0;
        if (!int.TryParse(hundredthsPart, out var hundredths))
            hundredths = 0;
        if (seconds <= 0 && hundredths <= 0)
            return null;
        return seconds * 100 + hundredths;
    }

    public static SwimTimeDualFieldUpdate FromClockText(string? text)
    {
        var hundredths = ParseDigits(text);
        return new SwimTimeDualFieldUpdate(
            hundredths,
            Format(hundredths),
            FormatSecondsField(hundredths));
    }

    public static SwimTimeDualFieldUpdate FromSecondsText(string? text)
    {
        var hundredths = ParseSecondsField(text);
        return new SwimTimeDualFieldUpdate(
            hundredths,
            Format(hundredths),
            FormatSecondsField(hundredths));
    }

    public static SwimTimeDualFieldUpdate FromHundredths(int? hundredths) =>
        new(hundredths, Format(hundredths), FormatSecondsField(hundredths));

    private static int? ParseDigitsFromDigitsOnly(string digits)
    {
        if (string.IsNullOrWhiteSpace(digits))
            return null;
        var normalized = digits.TrimStart('0');
        if (normalized.Length == 0)
            return null;
        if (normalized.Length > 9)
            normalized = normalized[^9..];
        var padded = normalized.PadLeft(4, '0');
        var hundredthsPart = padded[^2..];
        var secondsPart = padded[^4..^2];
        var minutesPart = padded.Length > 4 ? padded[..^4] : "0";
        if (!int.TryParse(minutesPart, out var minutes))
            minutes = 0;
        if (!int.TryParse(secondsPart, out var seconds))
            seconds = 0;
        if (!int.TryParse(hundredthsPart, out var hundredths))
            hundredths = 0;
        var total = minutes * 6000 + seconds * 100 + hundredths;
        return total == 0 ? null : total;
    }
}

public readonly record struct SwimTimeTextChange(string Text, int? Hundredths);

public readonly record struct SwimTimeDualFieldUpdate(
    int? Hundredths,
    string ClockText,
    string SecondsText);
