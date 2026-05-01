using DataLayer.EfCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace BizLogic.ReportGenerator.Concrete.Excel;

public class StartListReportExcel(EfCoreContext dbContext) : BaseReportExcel(dbContext)
{
    public override void AddWorksheet(ExcelPackage package, List<int> swimEventIds)
    {
        var swimEvents = DbAccess.GetSwimEventsWithHeats(swimEventIds);
        var worksheet = package.Workbook.Worksheets.Add("Стартовый протокол");
        RenderToWorksheet(worksheet, swimEvents);
    }

    private static void RenderToWorksheet(ExcelWorksheet worksheet,
        IEnumerable<DataLayer.EfClasses.SwimEvent> swimEvents)
    {
        const int colLane = 1;
        const int colParticipant = 2;
        const int colBirthYear = 3;
        const int colTeam = 4;
        const int colEntryTime = 5;
        const int tableLastCol = colEntryTime;
        
        worksheet.Cells.Style.Font.Name = "Calibri";
        worksheet.Cells.Style.Font.Size = 11;

        worksheet.Column(colLane).Width = 10;
        worksheet.Column(colParticipant).Width = 30;
        worksheet.Column(colBirthYear).Width = 10;
        worksheet.Column(colTeam).Width = 30;
        worksheet.Column(colEntryTime).Width = 10;
        
        worksheet.Column(colLane).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colBirthYear).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colEntryTime).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        var row = 1;
        foreach (var swimEvent in swimEvents)
        {
            if (row > 1) row += 1;

            var titleRange = worksheet.Cells[row, colLane, row, tableLastCol];
            titleRange.Merge = true;
            titleRange.Value = swimEvent.DisplayName;
            titleRange.Style.Font.Bold = true;
            titleRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            row += 1;
            
            worksheet.Cells[row, colLane].Value = "Дорожка";
            worksheet.Cells[row, colParticipant].Value = "Участник";
            worksheet.Cells[row, colBirthYear].Value = "Год рождения";
            worksheet.Cells[row, colTeam].Value = "Команда";
            worksheet.Cells[row, colEntryTime].Value = "Время";

            var headerRange = worksheet.Cells[row, colLane, row, tableLastCol];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            headerRange.Style.WrapText = true;
            row += 1;
            
            var heatsCount = swimEvents.Sum(se => se.Heats.Count());

            foreach (var heat in swimEvent.Heats)
            {
                var heatTitleRange = worksheet.Cells[row, colLane, row, tableLastCol];
                heatTitleRange.Merge = true;
                heatTitleRange.Value = $"Заплыв {heat.Number} из {swimEvent.Heats.Count} ({heat.Order} из {heatsCount})";
                heatTitleRange.Style.Font.Bold = true;
                heatTitleRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                row += 1;

                foreach (var position in heat.Positions)
                {
                    var entry = position.Entry;
                    var athlete = entry.Athlete;

                    worksheet.Cells[row, colLane].Value = position.Lane;
                    worksheet.Cells[row, colParticipant].Value = athlete?.DisplayName ?? "(нет данных)";
                    worksheet.Cells[row, colBirthYear].Value = athlete?.YearOfBirth;
                    worksheet.Cells[row, colTeam].Value = athlete?.DisplayClubName ?? "(Лично)";
                    worksheet.Cells[row, colEntryTime].Value = entry.DisplayEntryTime;

                    var dataRange = worksheet.Cells[row, colLane, row, tableLastCol];
                    dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    row += 1;
                }
            }
        }

        if (worksheet.Dimension is null) return;

        worksheet.View.FreezePanes(2, 1);
        worksheet.Cells[worksheet.Dimension.Address].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }
}