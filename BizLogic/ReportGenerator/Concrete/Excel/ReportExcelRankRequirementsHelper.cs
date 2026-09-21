using DataLayer.EfClasses;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BizLogic.ReportGenerator.Concrete.Excel;

internal static class ReportExcelRankRequirementsHelper
{
    public static void ApplyThinBorder(ExcelRange range)
    {
        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
    }

    public static int TryWriteAfterTitle(
        ExcelWorksheet worksheet,
        int row,
        int firstCol,
        int lastCol,
        SwimEvent swimEvent,
        IAchievedRankFormatter formatter)
    {
        var text = formatter.FormatRequirements(swimEvent);
        if (string.IsNullOrWhiteSpace(text))
            return row;

        var range = worksheet.Cells[row, firstCol, row, lastCol];
        range.Merge = true;
        range.Value = text;
        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ApplyThinBorder(range);
        return row + 1;
    }
}
