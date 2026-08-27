using BizLogic.ReportGenerator;
using BizLogic.Resources;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BizLogic.ReportGenerator.Concrete.Excel;

public static class CombinedResultsReportExcel
{
    public static void RenderToWorksheet(
        ExcelWorksheet worksheet,
        IEnumerable<(string AgeGroupTitle, CombinedResultsReportData Data)> sections)
    {
        const int colPlace = 1;
        const int colParticipant = 2;
        const int colBirthYear = 3;
        const int colTeam = 4;
        const int colTotal = 5;
        worksheet.Cells.Style.Font.Name = "Calibri";
        worksheet.Cells.Style.Font.Size = 11;
        worksheet.Column(colPlace).Width = 5;
        worksheet.Column(colParticipant).Width = 30;
        worksheet.Column(colBirthYear).Width = 10;
        worksheet.Column(colTeam).Width = 30;
        worksheet.Column(colTotal).Width = 10;
        worksheet.Column(colPlace).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colBirthYear).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colTotal).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        var row = 1;
        foreach (var (ageGroupTitle, data) in sections)
        {
            if (row > 1)
                row += 1;
            var eventColumnCount = data.EventColumns.Count;
            var tableLastCol = colTotal + eventColumnCount;
            for (var columnIndex = colTotal + 1; columnIndex <= tableLastCol; columnIndex++)
            {
                worksheet.Column(columnIndex).Width = 12;
                worksheet.Column(columnIndex).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            var titleRange = worksheet.Cells[row, colPlace, row, tableLastCol];
            titleRange.Merge = true;
            titleRange.Value = ageGroupTitle;
            titleRange.Style.Font.Bold = true;
            titleRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            row += 1;
            worksheet.Cells[row, colPlace].Value = ReportExcelStrings.Col_No;
            worksheet.Cells[row, colParticipant].Value = ReportExcelStrings.Col_Participant;
            worksheet.Cells[row, colBirthYear].Value = ReportExcelStrings.Col_BirthYear;
            worksheet.Cells[row, colTeam].Value = ReportExcelStrings.Col_Team;
            worksheet.Cells[row, colTotal].Value = ReportExcelStrings.Col_Total;
            var eventColumnOffset = colTotal;
            foreach (var eventColumn in data.EventColumns)
                worksheet.Cells[row, ++eventColumnOffset].Value = eventColumn.Header;
            var headerRange = worksheet.Cells[row, colPlace, row, tableLastCol];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            headerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            headerRange.Style.WrapText = true;
            row += 1;
            foreach (var athleteRow in data.Athletes)
            {
                if (athleteRow.Place is int placeValue)
                    worksheet.Cells[row, colPlace].Value = placeValue;
                worksheet.Cells[row, colParticipant].Value = athleteRow.ParticipantName;
                worksheet.Cells[row, colBirthYear].Value = athleteRow.YearOfBirth;
                worksheet.Cells[row, colTeam].Value = athleteRow.ClubName;
                worksheet.Cells[row, colTotal].Value = athleteRow.TotalPoints;
                eventColumnOffset = colTotal;
                foreach (var eventColumn in data.EventColumns)
                {
                    var cell = worksheet.Cells[row, ++eventColumnOffset];
                    cell.Value = athleteRow.PointsBySwimStyleId.GetValueOrDefault(eventColumn.SwimStyleId);
                    if (IsNonScoringSwimStyle(athleteRow, eventColumn.SwimStyleId))
                        ReportExcelScoringHelper.ApplyNonScoringFill(cell);
                }
                var dataRange = worksheet.Cells[row, colPlace, row, tableLastCol];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                row += 1;
            }
        }
        if (worksheet.Dimension is null)
            return;
        worksheet.Cells[worksheet.Dimension.Address].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }

    private static bool IsNonScoringSwimStyle(CombinedResultsReportAthleteRow athleteRow, int swimStyleId) =>
        athleteRow.ScoringBySwimStyleId.TryGetValue(swimStyleId, out var scoring) && !scoring;
}
