using BizDbAccess;
using DataLayer.EfCore;
using OfficeOpenXml;

namespace BizLogic.ReportGenerator.Concrete.Excel;

public abstract class BaseReportExcel(EfCoreContext dbContext)
{
    protected readonly ReportGeneratorDbAccess DbAccess = new ReportGeneratorDbAccess(dbContext);
    public abstract void AddWorksheet(ExcelPackage package, List<int> swimEventIds);

}