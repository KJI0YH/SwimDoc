using BizLogic.EntryDocumentReader;
using ServiceLayer.AppSettings;
using ServiceLayer.Logging;

namespace ServiceLayer.EntryImportSettings;

public sealed class EntryImportSettingsService : IEntryImportSettingsService
{
    private static readonly EntryImportHighlightScoringMode DefaultMode =
        EntryImportHighlightScoringMode.HighlightedInCompetition;

    private readonly IAppSettingsStore _settingsStore;
    private readonly IAppLog _log;
    private EntryImportHighlightScoringMode _highlightScoringMode = DefaultMode;

    public EntryImportHighlightScoringMode HighlightScoringMode => _highlightScoringMode;

    public EntryImportSettingsService(IAppSettingsStore settingsStore)
        : this(settingsStore, NullAppLog.Instance)
    {
    }

    public EntryImportSettingsService(IAppSettingsStore settingsStore, IAppLog log)
    {
        _settingsStore = settingsStore;
        _log = log;
        LoadFromStore();
    }

    public EntryImportHighlightScoringMode SetHighlightScoringMode(EntryImportHighlightScoringMode mode)
    {
        if (_highlightScoringMode == mode)
            return mode;
        _highlightScoringMode = mode;
        SaveToStore();
        _log.Info($"Changed entry import highlight scoring mode: {mode}");
        return mode;
    }

    private void LoadFromStore()
    {
        var modeText = _settingsStore.Get().EntryImportHighlightScoringMode;
        if (Enum.TryParse(modeText, ignoreCase: true, out EntryImportHighlightScoringMode mode))
            _highlightScoringMode = mode;
    }

    private void SaveToStore()
    {
        _settingsStore.Update(settings =>
            settings.EntryImportHighlightScoringMode = _highlightScoringMode.ToString());
    }
}
