using DataLayer.Display;
using DataLayer.EfCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using BizLogic.Helpers;
using BizLogic.Resources;

namespace BizLogic.ReportGenerator.Concrete.Excel;

public class FinishListReportExcel(EfCoreContext dbContext, IAchievedRankFormatter? achievedRankFormatter = null)
    : BaseReportExcel(dbContext)
{
    private readonly IAchievedRankFormatter _achievedRankFormatter =
        achievedRankFormatter ?? NullAchievedRankFormatter.Instance;

    public override void AddWorksheet(ExcelPackage package, List<int> swimEventIds)
    {
        var swimEvents = DbAccess.GetSwimEventsWithResults(swimEventIds);
        var worksheet = package.Workbook.Worksheets.Add(ReportExcelStrings.Sheet_FinishList);
        RenderToWorksheet(worksheet, swimEvents);
    }

    private void RenderToWorksheet(ExcelWorksheet worksheet, IEnumerable<DataLayer.EfClasses.SwimEvent> swimEvents)
    {
        const int colNo = 1;
        const int colParticipant = 2;
        const int colBirthYear = 3;
        const int colCategory = 4;
        const int colTeam = 5;
        const int colFinishTime = 6;
        const int colRank = 7;
        const int colPoints = 8;
        const int colComment = 9;
        const int tableLastCol = colComment;
        worksheet.Cells.Style.Font.Name = "Calibri";
        worksheet.Cells.Style.Font.Size = 11;
        worksheet.Column(colNo).Width = 5;
        worksheet.Column(colParticipant).Width = 30;
        worksheet.Column(colBirthYear).Width = 10;
        worksheet.Column(colCategory).Width = 12;
        worksheet.Column(colTeam).Width = 30;
        worksheet.Column(colFinishTime).Width = 10;
        worksheet.Column(colRank).Width = 10;
        worksheet.Column(colPoints).Width = 10;
        worksheet.Column(colComment).Width = 50;
        worksheet.Column(colNo).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colBirthYear).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colCategory).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colFinishTime).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colRank).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Column(colPoints).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        var row = 1;
        foreach (var swimEvent in swimEvents)
        {
            if (row > 1) row += 1;
            var titleRange = worksheet.Cells[row, colNo, row, tableLastCol];
            titleRange.Merge = true;
            titleRange.Value = LocalizedEntityDisplayFormatter.FormatSwimEvent(swimEvent);
            titleRange.Style.Font.Bold = true;
            titleRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ReportExcelRankRequirementsHelper.ApplyThinBorder(titleRange);
            if (ReportExcelScoringHelper.IsNonScoringSwimEvent(swimEvent))
                ReportExcelScoringHelper.ApplyNonScoringFill(titleRange);
            row += 1;
            row = ReportExcelRankRequirementsHelper.TryWriteAfterTitle(
                worksheet, row, colNo, tableLastCol, swimEvent, _achievedRankFormatter);
            worksheet.Cells[row, colNo].Value = ReportExcelStrings.Col_No;
            worksheet.Cells[row, colParticipant].Value = ReportExcelStrings.Col_Participant;
            worksheet.Cells[row, colBirthYear].Value = ReportExcelStrings.Col_BirthYear;
            worksheet.Cells[row, colCategory].Value = ReportExcelStrings.Col_Category;
            worksheet.Cells[row, colTeam].Value = ReportExcelStrings.Col_Team;
            worksheet.Cells[row, colFinishTime].Value = ReportExcelStrings.Col_Time;
            worksheet.Cells[row, colRank].Value = ReportExcelStrings.Col_Rank;
            worksheet.Cells[row, colPoints].Value = ReportExcelStrings.Col_Points;
            worksheet.Cells[row, colComment].Value = ReportExcelStrings.Col_Comment;
            var headerRange = worksheet.Cells[row, colNo, row, tableLastCol];
            headerRange.Style.Font.Bold = true;
            ReportExcelRankRequirementsHelper.ApplyThinBorder(headerRange);
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            headerRange.Style.WrapText = true;
            row += 1;
            if (swimEvent.Entries.Count == 0)
                continue;
            var entries = EntryPlaceAssignment.OrderForResults(swimEvent.Entries);
            foreach (var (entry, entryPlace) in EntryPlaceAssignment.AssignPlaces(entries))
            {
                worksheet.Cells[row, colNo].Value = EntryTimeDisplay.FormatResultPlace(entry, entryPlace);
                worksheet.Cells[row, colParticipant].Value = LocalizedEntityDisplayFormatter.FormatEntryParticipantName(entry);
                worksheet.Cells[row, colBirthYear].Value = LocalizedEntityDisplayFormatter.FormatEntryParticipantBirthYear(entry);
                worksheet.Cells[row, colCategory].Value = LocalizedEntityDisplayFormatter.FormatEntryParticipantCategory(entry);
                worksheet.Cells[row, colTeam].Value = LocalizedEntityDisplayFormatter.FormatEntryParticipantClubName(entry);
                worksheet.Cells[row, colFinishTime].Value = EntryTimeDisplay.FormatResultTime(entry);
                worksheet.Cells[row, colRank].Value = _achievedRankFormatter.Format(swimEvent, entry);
                worksheet.Cells[row, colPoints].Value = entry.Points;
                worksheet.Cells[row, colComment].Value = entry.Comment;
                var dataRange = worksheet.Cells[row, colNo, row, tableLastCol];
                ReportExcelRankRequirementsHelper.ApplyThinBorder(dataRange);
                if (ReportExcelScoringHelper.IsNonScoringEntry(entry))
                    ReportExcelScoringHelper.ApplyNonScoringFill(dataRange);
                row += 1;
            }
        }
        if (worksheet.Dimension is null) return;
        worksheet.Cells[worksheet.Dimension.Address].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }
}
