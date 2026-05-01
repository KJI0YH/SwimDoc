using BizLogic.ReportGenerator.Concrete.Excel;
using DataLayer.EfCore;
using OfficeOpenXml;

namespace ServiceLayer.ReportGeneratorService;

public sealed class ReportExportService(EfCoreContext dbContext) : IReportExportService
{
    public void ExportToExcel(ReportExportOptions options)
    {
        if (options.SwimEventIds.Count == 0)
            throw new ArgumentException("No swim events selected.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.OutputFilePath))
            throw new ArgumentException("Output file path is empty.", nameof(options));

        var any = options.IncludeEntryList || options.IncludeStartList || options.IncludeFinishList;
        if (!any)
            throw new ArgumentException("No reports selected.", nameof(options));

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