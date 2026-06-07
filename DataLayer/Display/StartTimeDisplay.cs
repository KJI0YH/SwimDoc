namespace DataLayer.Display;

public static class StartTimeDisplay
{
    public const string NotSetText = "--:--";

    public static bool IsSet(TimeOnly? time) =>
        time is { } value && value != TimeOnly.MinValue;

    public static string Format(TimeOnly? time, string format = "HH:mm") =>
        IsSet(time) ? time!.Value.ToString(format) : NotSetText;
}
