using DataLayer.EfClasses;

namespace ServiceLayer.EntryService;

public sealed record CombinedResultsEventColumn(int SwimStyleId, string Header, bool HasScoringEntries);
