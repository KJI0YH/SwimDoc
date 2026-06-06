namespace ServiceLayer.ReportGeneratorService;

public sealed class ReportExportOptions
{
    public required IReadOnlyList<int> SwimEventIds { get; init; }
    public required string OutputFilePath { get; init; }

    public bool IncludeEntryList { get; init; }
    public bool IncludeStartList { get; init; }
    public bool IncludeFinishList { get; init; }
}
