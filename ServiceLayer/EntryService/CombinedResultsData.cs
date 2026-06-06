namespace ServiceLayer.EntryService;

public sealed record CombinedResultsData(
    IReadOnlyList<CombinedResultsEventColumn> EventColumns,
    IReadOnlyList<CombinedResultsAthleteRow> Athletes);
