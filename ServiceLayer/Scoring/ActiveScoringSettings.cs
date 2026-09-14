namespace ServiceLayer.Scoring;

public sealed class ActiveScoringSettings
{
    public static IReadOnlyList<int> DefaultPlacePoints { get; } =
        [50, 45, 40, 36, 32, 28, 25, 22, 19, 16, 14, 12, 10, 8, 6, 5, 4, 3, 2, 1];

    public ScoringMode Mode { get; init; } = ScoringMode.WorldAquatics;
    public IReadOnlyList<int> PlacePoints { get; init; } = DefaultPlacePoints;

    public static ActiveScoringSettings Defaults { get; } = new()
    {
        Mode = ScoringMode.WorldAquatics,
        PlacePoints = DefaultPlacePoints
    };
}
