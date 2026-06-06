namespace BizLogic.ReportGenerator;

public sealed record CombinedResultsReportData(
    IReadOnlyList<CombinedResultsReportEventColumn> EventColumns,
    IReadOnlyList<CombinedResultsReportAthleteRow> Athletes);
