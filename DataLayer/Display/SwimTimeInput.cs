using System.Globalization;

namespace DataLayer.Display;

public static class SwimTimeInput
{
    private const int MaxDigitCount = 9;

    public static string Format(int? hundredths) =>
        hundredths.HasValue ? EntryTimeDisplay.FormatHundredths(hundredths.Value) : string.Empty;

    public static string ExtractDigits(string? text) =>
        new string((text ?? string.Empty).Where(char.IsDigit).ToArray());

    public static int? ParseDigits(string? text) =>
        ParseDigitsFromDigitsOnly(ExtractDigits(text));

    public static string ToDigitBuffer(int? hundredths)
    {
        if (hundredths is null or <= 0)
            return string.Empty;
        var value = hundredths.Value;
        var minutes = value / 6000;
        var seconds = value % 6000 / 100;
        var frac = value % 100;
        if (minutes > 0)
            return $"{minutes}{seconds:D2}{frac:D2}";
        if (seconds > 0)
            return $"{seconds}{frac:D2}";
        return frac.ToString(CultureInfo.InvariantCulture);
    }

    public static SwimTimeTextChange ApplyText(string? text) =>
        ApplyText(text, previousDigits: string.Empty, previousDisplay: string.Empty);

    public static SwimTimeTextChange ApplyText(string? text, string previousDigits, string previousDisplay)
    {
        var digits = ResolveTypedDigits(text, previousDigits, previousDisplay);
        var hundredths = ParseDigitsFromDigitsOnly(digits);
        return new SwimTimeTextChange(Format(hundredths), hundredths, digits);
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
        if (normalized.Length > MaxDigitCount)
            normalized = normalized[^MaxDigitCount..];
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

    public static SwimTimeDualFieldUpdate FromClockText(string? text) =>
        FromClockText(text, previousDigits: string.Empty, previousDisplay: string.Empty);

    public static SwimTimeDualFieldUpdate FromClockText(string? text, string previousDigits, string previousDisplay)
    {
        var digits = ResolveTypedDigits(text, previousDigits, previousDisplay);
        var hundredths = ParseDigitsFromDigitsOnly(digits);
        return new SwimTimeDualFieldUpdate(
            hundredths,
            Format(hundredths),
            FormatSecondsField(hundredths),
            digits);
    }

    public static SwimTimeDualFieldUpdate FromSecondsText(string? text)
    {
        var hundredths = ParseSecondsField(text);
        return new SwimTimeDualFieldUpdate(
            hundredths,
            Format(hundredths),
            FormatSecondsField(hundredths),
            ToDigitBuffer(hundredths));
    }

    public static SwimTimeDualFieldUpdate FromHundredths(int? hundredths) =>
        new(hundredths, Format(hundredths), FormatSecondsField(hundredths), ToDigitBuffer(hundredths));

    public static string ResolveTypedDigits(string? newText, string previousDigits, string previousDisplay)
    {
        newText ??= string.Empty;
        previousDigits ??= string.Empty;
        previousDisplay ??= string.Empty;

        if (previousDisplay.Length > 0
            && newText.Length == previousDisplay.Length + 1
            && newText.StartsWith(previousDisplay, StringComparison.Ordinal)
            && char.IsDigit(newText[^1]))
        {
            return ClampDigits(previousDigits + newText[^1]);
        }

        if (previousDisplay.Length > 0
            && newText.Length == previousDisplay.Length - 1
            && previousDisplay.StartsWith(newText, StringComparison.Ordinal))
        {
            return previousDigits.Length > 0 ? previousDigits[..^1] : string.Empty;
        }

        return ClampDigits(ExtractDigits(newText));
    }

    private static string ClampDigits(string digits)
    {
        if (digits.Length <= MaxDigitCount)
            return digits;
        return digits[^MaxDigitCount..];
    }

    private static int? ParseDigitsFromDigitsOnly(string digits)
    {
        if (string.IsNullOrWhiteSpace(digits))
            return null;
        var normalized = digits.TrimStart('0');
        if (normalized.Length == 0)
            return null;
        if (normalized.Length > MaxDigitCount)
            normalized = normalized[^MaxDigitCount..];
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

public readonly record struct SwimTimeTextChange(string Text, int? Hundredths, string Digits = "");

public readonly record struct SwimTimeDualFieldUpdate(
    int? Hundredths,
    string ClockText,
    string SecondsText,
    string Digits = "");
