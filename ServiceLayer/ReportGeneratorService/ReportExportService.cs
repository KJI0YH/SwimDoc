using BizLogic.Helpers;
using BizLogic.ReportGenerator;
using BizLogic.ReportGenerator.Concrete.Excel;
using BizLogic.Resources;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ServiceLayer.EntryService;
using ServiceLayer.Resources;

namespace ServiceLayer.ReportGeneratorService;

public sealed class ReportExportService(EfCoreContext dbContext, IEntryService entryService) : IReportExportService
{
    public void ExportToExcel(ReportExportOptions options)
    {
        if (options.SwimEventIds.Count == 0)
            throw new ArgumentException(ServiceErrorStrings.ReportExport_NoSwimEventsSelected, nameof(options));
        if (string.IsNullOrWhiteSpace(options.OutputFilePath))
            throw new ArgumentException(ServiceErrorStrings.ReportExport_OutputPathEmpty, nameof(options));
        var any = options.IncludeEntryList || options.IncludeStartList || options.IncludeFinishList;
        if (!any)
            throw new ArgumentException(ServiceErrorStrings.ReportExport_NoReportsSelected, nameof(options));
        using var package = new ExcelPackage();
        if (options.IncludeEntryList)
            new EntryListReportExcel(dbContext).AddWorksheet(package, options.SwimEventIds.ToList());
        if (options.IncludeStartList)
            new StartListReportExcel(dbContext).AddWorksheet(package, options.SwimEventIds.ToList());
        if (options.IncludeFinishList)
            new FinishListReportExcel(dbContext).AddWorksheet(package, options.SwimEventIds.ToList());
        package.SaveAs(new FileInfo(options.OutputFilePath));
    }

    public void ExportCombinedResultsToExcel(CombinedResultsExportOptions options)
    {
        if (options.AgeGroupIds.Count == 0)
            throw new ArgumentException(ServiceErrorStrings.ReportExport_NoAgeGroupsSelected, nameof(options));
        if (string.IsNullOrWhiteSpace(options.OutputFilePath))
            throw new ArgumentException(ServiceErrorStrings.ReportExport_OutputPathEmpty, nameof(options));
        var ageGroupsById = dbContext.AgeGroups
            .AsNoTracking()
            .Where(ageGroup => options.AgeGroupIds.Contains(ageGroup.Id))
            .ToDictionary(ageGroup => ageGroup.Id);
        var sections = new List<(string AgeGroupTitle, CombinedResultsReportData Data)>();
        foreach (var ageGroupId in options.AgeGroupIds)
        {
            if (!ageGroupsById.TryGetValue(ageGroupId, out var ageGroup))
                continue;
            var data = entryService.GetCombinedResultsByAgeGroupAsync(ageGroupId).GetAwaiter().GetResult();
            sections.Add((LocalizedEntityDisplayFormatter.FormatAgeGroup(ageGroup), MapReportData(data)));
        }
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(ReportExcelStrings.Sheet_CombinedResults);
        CombinedResultsReportExcel.RenderToWorksheet(worksheet, sections);
        package.SaveAs(new FileInfo(options.OutputFilePath));
    }

    private static CombinedResultsReportData MapReportData(CombinedResultsData data)
    {
        var reportAthletes = CombinedResultsCalculator.AssignPlaces(data.Athletes)
            .Select(item => MapAthleteRow(item.AthleteRow, item.Place))
            .ToList();
        return new CombinedResultsReportData(
            data.EventColumns
                .Select(column => new CombinedResultsReportEventColumn(
                    column.SwimStyleId,
                    column.Header,
                    column.HasScoringEntries))
                .ToList(),
            reportAthletes);
    }

    private static CombinedResultsReportAthleteRow MapAthleteRow(CombinedResultsAthleteRow row, int place) =>
        new(
            LocalizedEntityDisplayFormatter.FormatAthleteName(row.Athlete),
            row.Athlete.YearOfBirth,
            LocalizedEntityDisplayFormatter.FormatAthleteClubName(row.Athlete),
            row.PointsBySwimStyleId,
            row.ScoringBySwimStyleId,
            row.TotalPoints,
            row.IsInOfficialStandings,
            place);
}
