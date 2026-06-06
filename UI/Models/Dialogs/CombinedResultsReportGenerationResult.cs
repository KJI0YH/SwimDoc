namespace UI.Models.Dialogs;

public sealed class CombinedResultsReportGenerationResult(string outputFilePath)
{
    public string OutputFilePath { get; } = outputFilePath;
}
