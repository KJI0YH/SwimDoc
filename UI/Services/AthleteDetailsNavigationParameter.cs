namespace UI.Services;

public sealed class AthleteDetailsNavigationParameter
{
    public required int AthleteId { get; init; }

    public int? FocusEntryId { get; init; }

    public int? FocusSwimEventId { get; init; }

    public bool OpenHeatsTab => FocusEntryId.HasValue;
}
