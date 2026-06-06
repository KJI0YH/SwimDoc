namespace ServiceLayer.EntryDocumentReaderService;

public readonly record struct EntryImportStats(
    int ClubsScanned,
    int ClubsWithErrors,
    int ClubsAdded,
    int ClubsUpdated,
    int AthletesScanned,
    int AthletesWithErrors,
    int AthletesAdded,
    int AthletesUpdated,
    int EntriesScanned,
    int EntriesWithErrors,
    int EntriesAdded,
    int EntriesUpdated);
