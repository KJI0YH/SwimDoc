using DataLayer.EfClasses;

namespace ServiceLayer.EntryService;

public sealed record CombinedResultsEventColumn(int EventId, string Header, bool HasScoringEntries);
