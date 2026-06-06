namespace ServiceLayer.ReportGeneratorService;

public sealed class CombinedResultsExportOptions
{
    public required IReadOnlyList<int> AgeGroupIds { get; init; }
    public required string OutputFilePath { get; init; }
}
