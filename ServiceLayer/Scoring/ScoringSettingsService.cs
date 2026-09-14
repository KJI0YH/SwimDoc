using ServiceLayer.AppSettings;
using ServiceLayer.Logging;

namespace ServiceLayer.Scoring;

public sealed class ScoringSettingsService : IScoringSettingsService
{
    private static readonly int[] LegacyDefaultPlacePoints = [9, 7, 6, 5, 4, 3, 2, 1];

    private readonly IAppSettingsStore _settingsStore;
    private readonly IAppLog _log;
    private ActiveScoringSettings _current;

    public ActiveScoringSettings Current => _current;

    public event Action? Changed;

    public ScoringSettingsService(IAppSettingsStore settingsStore, IAppLog log)
    {
        _settingsStore = settingsStore;
        _log = log;
        _current = LoadFromStore();
    }

    public void SetActive(ActiveScoringSettings settings, bool raiseChanged = true)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _current = Normalize(settings);
        SaveToStore(_current);
        _log.Info($"Scoring settings updated: mode={_current.Mode}");
        if (raiseChanged)
            Changed?.Invoke();
    }

    private ActiveScoringSettings LoadFromStore()
    {
        var stored = _settingsStore.Get();
        var mode = ScoringMode.WorldAquatics;
        if (!string.IsNullOrWhiteSpace(stored.ScoringMode) &&
            Enum.TryParse(stored.ScoringMode, ignoreCase: true, out ScoringMode parsed) &&
            Enum.IsDefined(parsed))
            mode = parsed;
        else if (string.Equals(stored.ScoringMode, "CustomFormula", StringComparison.OrdinalIgnoreCase))
            mode = ScoringMode.WorldAquatics;

        return Normalize(new ActiveScoringSettings
        {
            Mode = mode,
            PlacePoints = ResolvePlacePoints(stored.PlacePoints)
        });
    }

    private static List<int> ResolvePlacePoints(List<int>? stored)
    {
        if (stored is null || stored.Count == 0)
            return ActiveScoringSettings.DefaultPlacePoints.ToList();

        if (stored.SequenceEqual(LegacyDefaultPlacePoints))
            return ActiveScoringSettings.DefaultPlacePoints.ToList();

        return stored.ToList();
    }

    private void SaveToStore(ActiveScoringSettings settings)
    {
        _settingsStore.Update(s =>
        {
            s.ScoringMode = settings.Mode.ToString();
            s.PlacePoints = settings.PlacePoints.ToList();
            s.ActiveScoringPresetId = null;
        });
    }

    private static ActiveScoringSettings Normalize(ActiveScoringSettings settings) =>
        new()
        {
            Mode = settings.Mode,
            PlacePoints = settings.PlacePoints.Count > 0
                ? settings.PlacePoints.ToList()
                : ActiveScoringSettings.DefaultPlacePoints.ToList()
        };
}
