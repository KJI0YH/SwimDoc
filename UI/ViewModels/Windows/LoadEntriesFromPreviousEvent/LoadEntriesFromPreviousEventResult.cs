namespace UI.ViewModels.Windows.LoadEntriesFromPreviousEvent;

public sealed class LoadEntriesFromPreviousEventResult(
    int previousEventId,
    int targetEventId,
    string previousEventDisplayName,
    string targetEventDisplayName)
{
    public int PreviousEventId { get; } = previousEventId;
    public int TargetEventId { get; } = targetEventId;
    public string PreviousEventDisplayName { get; } = previousEventDisplayName;
    public string TargetEventDisplayName { get; } = targetEventDisplayName;
}
