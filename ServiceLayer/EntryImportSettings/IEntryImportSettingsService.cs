using BizLogic.EntryDocumentReader;

namespace ServiceLayer.EntryImportSettings;

public interface IEntryImportSettingsService
{
    EntryImportHighlightScoringMode HighlightScoringMode { get; }
    EntryImportHighlightScoringMode SetHighlightScoringMode(EntryImportHighlightScoringMode mode);
}
