namespace UI.ViewModels.Windows.CombinedResultsReportGeneration;

public sealed class CombinedResultsReportGenerationResult(string outputFilePath)
{
    public string OutputFilePath { get; } = outputFilePath;
}
