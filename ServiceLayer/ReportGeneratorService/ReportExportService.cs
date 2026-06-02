using BizLogic.ReportGenerator.Concrete.Excel;
using DataLayer.EfCore;
using OfficeOpenXml;
using ServiceLayer.Resources;

namespace ServiceLayer.ReportGeneratorService;

public sealed class ReportExportService(EfCoreContext dbContext) : IReportExportService
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
}