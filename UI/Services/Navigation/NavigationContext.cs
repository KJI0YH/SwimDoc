namespace UI.Services.Navigation;

public sealed record NavigationContext
{
    public int? Id { get; init; }
    public int? ClubId { get; init; }
    public int? AthleteId { get; init; }
    public int? EventId { get; init; }
    public int? EntryId { get; init; }
    public int? AgeGroupId { get; init; }
    public int? SwimStyleId { get; init; }
    public int? FocusEntryId { get; init; }
    public int? FocusSwimEventId { get; init; }

    public bool OpenHeatsTab => FocusEntryId.HasValue;
    public static NavigationContext ForId(int id) => new() { Id = id };
    public static NavigationContext ForAthlete(int athleteId, int? focusEntryId = null, int? focusSwimEventId = null) =>
        new()
        {
            Id = athleteId,
            AthleteId = athleteId,
            FocusEntryId = focusEntryId,
            FocusSwimEventId = focusSwimEventId
        };

    public static NavigationContext? Parse(object? parameter) =>
        parameter switch
        {
            null => null,
            NavigationContext context => context,
            int id => ForId(id),
            _ => null
        };

    public static NavigationContext Merge(int? id, NavigationContext? context)
    {
        if (context is null)
            return id.HasValue ? ForId(id.Value) : new NavigationContext();
        return id.HasValue ? context with { Id = id } : context;
    }

    public int? ResolveId() => Id;
}
