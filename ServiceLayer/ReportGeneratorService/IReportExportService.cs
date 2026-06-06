namespace ServiceLayer.ReportGeneratorService;

public interface IReportExportService
{
    void ExportToExcel(ReportExportOptions options);
    void ExportCombinedResultsToExcel(CombinedResultsExportOptions options);
}
