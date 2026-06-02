using DataLayer.EfCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using BizLogic.Helpers;
using BizLogic.Resources;

namespace BizLogic.ReportGenerator.Concrete.Excel;

public class EntryListReportExcel(EfCoreContext dbContext) : BaseReportExcel(dbContext)
{
    public override void AddWorksheet(ExcelPackage package, List<int> swimEventIds)
    {
        var swimEvents = DbAccess.GetSwimEventsWithEntries(swimEventIds);
        var worksheet = package.Workbook.Worksheets.Add(ReportExcelStrings.Sheet_EntryList);
        RenderToWorksheet(worksheet, swimEvents);
    }

    private static void RenderToWorksheet(ExcelWorksheet worksheet, IEnumerable<DataLayer.EfClasses.SwimEvent> swimEvents)
    {
        const int colNo = 1;
        const int colParticipant = 2;
        const int colBirthYear = 3;
        const int colTeam = 4;
        const int colEntryTime = 5;
        const int tableLastCol = colEntryTime;
        
        worksheet.Cells.Style.Font.Name = "Calibri";
        worksheet.Cells.Style.Font.Size = 11;

        worksheet.Column(colNo).Width = 5;
        worksheet.Column(colParticipant).Width = 30;
        worksheet.Column(colBirthYear).Width = 10;
        worksheet.Column(colTeam).Width = 30;
        worksheet.Column(colEntryTime).Width = 10;
        
        worksheet.Column(colNo).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colBirthYear).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colEntryTime).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        var row = 1;
        foreach (var swimEvent in swimEvents)
        {
            if (row > 1) row += 1;

            var titleRange = worksheet.Cells[row, colNo, row, tableLastCol];
            titleRange.Merge = true;
            titleRange.Value = LocalizedEntityDisplayFormatter.FormatSwimEvent(swimEvent);
            titleRange.Style.Font.Bold = true;
            titleRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            row += 1;
            
            worksheet.Cells[row, colNo].Value = ReportExcelStrings.Col_No;
            worksheet.Cells[row, colParticipant].Value = ReportExcelStrings.Col_Participant;
            worksheet.Cells[row, colBirthYear].Value = ReportExcelStrings.Col_BirthYear;
            worksheet.Cells[row, colTeam].Value = ReportExcelStrings.Col_Team;
            worksheet.Cells[row, colEntryTime].Value = ReportExcelStrings.Col_Time;

            var headerRange = worksheet.Cells[row, colNo, row, tableLastCol];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            headerRange.Style.WrapText = true;
            row += 1;
            
            var place = 1;
            var prevEntry = swimEvent.Entries.First();
            var prevPlace = 1;
            foreach (var entry in swimEvent.Entries)
            {
                var athlete = entry.Athlete;
                
                if (prevEntry.EntryTime == entry.EntryTime)
                {
                    worksheet.Cells[row, colNo].Value = prevPlace;
                }
                else
                {
                    worksheet.Cells[row, colNo].Value = place;
                    prevPlace = place;
                }

                worksheet.Cells[row, colParticipant].Value = athlete?.DisplayName ?? ReportExcelStrings.Value_NoneParen;
                worksheet.Cells[row, colBirthYear].Value = athlete?.YearOfBirth;
                worksheet.Cells[row, colTeam].Value = athlete?.Club?.Name ?? ReportExcelStrings.Value_PersonalParen;
                worksheet.Cells[row, colEntryTime].Value = entry.DisplayEntryTime;

                var dataRange = worksheet.Cells[row, colNo, row, tableLastCol];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                prevEntry = entry;
                place++;
                row += 1;
            }
        }

        if (worksheet.Dimension is null) return;

        worksheet.View.FreezePanes(2, 1);
        worksheet.Cells[worksheet.Dimension.Address].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }
}