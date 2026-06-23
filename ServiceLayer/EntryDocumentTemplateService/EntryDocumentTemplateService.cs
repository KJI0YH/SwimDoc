using OfficeOpenXml;
using OfficeOpenXml.Style;
using ServiceLayer.Resources;

namespace ServiceLayer.EntryDocumentTemplateService;

public sealed class EntryDocumentTemplateService : IEntryDocumentTemplateService
{
    private const int FirstSwimStyleColumn = 6;
    private const int FirstEntryRow = 4;
    private const int ExcelMaxColumns = 16384;
    private const int ExcelMaxRows = 1048576;
    public byte[] CreateTemplate()
    {
        using var package = new ExcelPackage();
        var entriesSheet = package.Workbook.Worksheets.Add(TemplateStrings.Sheet_Entries);
        BuildEntriesSheet(entriesSheet);
        var settingsSheet = package.Workbook.Worksheets.Add(TemplateStrings.Sheet_Settings);
        BuildSettingsSheet(settingsSheet);
        ApplyValidations(entriesSheet);
        return package.GetAsByteArray();
    }

    private static void BuildEntriesSheet(ExcelWorksheet ws)
    {
        ws.Cells.Style.Font.Name = "Calibri";
        ws.Cells.Style.Font.Size = 11;
        ws.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        ws.Cells.Style.WrapText = true;
        ws.Column(1).Width = 30;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 15;
        ws.Column(4).Width = 20;
        ws.Column(5).Width = 15;
        ws.Column(6).Width = 15;
        ws.Column(8).Width = 10;
        ws.Row(3).Height = 15;
        ws.Row(4).Height = 18;
        ws.Cells["A1"].Value = TemplateStrings.Entries_Title_A1;
        ws.Cells["B1"].Value = TemplateStrings.Entries_Title_B1;
        ws.Cells["A2"].Value = TemplateStrings.Entries_Header_LastName;
        ws.Cells["B2"].Value = TemplateStrings.Entries_Header_FirstName;
        ws.Cells["C2"].Value = TemplateStrings.Entries_Header_BirthYear;
        ws.Cells["D2"].Value = TemplateStrings.Entries_Header_Gender;
        ws.Cells["E2"].Value = TemplateStrings.Entries_Header_Category;
        ws.Cells["F2"].Value = 50;
        ws.Cells["F3"].Value = TemplateStrings.Stroke_Free;
        ws.Cells["A4"].Value = TemplateStrings.Entries_Example_LastName;
        ws.Cells["B4"].Value = TemplateStrings.Entries_Example_FirstName;
        ws.Cells["C4"].Value = 2000;
        ws.Cells["D4"].Value = TemplateStrings.Gender_Female;
        ws.Cells["E4"].Value = TemplateStrings.Entries_Example_Category;
        ws.Cells["F4"].Style.Numberformat.Format = "@";
        ws.Cells["F4"].Value = "25.99";
        ws.Cells["A2:A3"].Merge = true;
        ws.Cells["B2:B3"].Merge = true;
        ws.Cells["C2:C3"].Merge = true;
        ws.Cells["D2:D3"].Merge = true;
        ws.Cells["E2:E3"].Merge = true;
        ApplyEntriesSheetStyles(ws);
    }

    private static void ApplyEntriesSheetStyles(ExcelWorksheet ws)
    {
        using var headerRange = ws.Cells["A1:A1"];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        ws.Cells["B1"].Style.WrapText = true;
        using var columnHeaders = ws.Cells["A2:F3"];
        columnHeaders.Style.Font.Bold = true;
        columnHeaders.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        columnHeaders.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        columnHeaders.Style.WrapText = true;
        ws.Row(2).Style.Font.Bold = true;
        ws.Row(3).Style.Font.Bold = true;
        ws.View.ShowGridLines = true;
    }

    private static void BuildSettingsSheet(ExcelWorksheet ws)
    {
        ws.Cells.Style.Font.Name = "Calibri";
        ws.Cells.Style.Font.Size = 11;
        ws.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        ws.Cells.Style.WrapText = true;
        ws.Column(1).Width = 27.81640625;
        ws.Column(3).Width = 18.08984375;
        ws.Column(4).Width = 30.54296875;
        ws.Column(5).Width = 11.0;
        ws.Row(1).Height = 29;
        ws.Cells["A1"].Value = TemplateStrings.Settings_Header_Gender;
        ws.Cells["B1"].Value = TemplateStrings.Settings_Header_Category;
        ws.Cells["C1"].Value = TemplateStrings.Settings_Header_Distance;
        ws.Cells["D1"].Value = TemplateStrings.Settings_Header_Stroke;
        ws.Cells["A2"].Value = TemplateStrings.Gender_Female;
        ws.Cells["A3"].Value = TemplateStrings.Gender_Male;
        ws.Cells["B2"].Value = TemplateStrings.Category_IMoS;
        ws.Cells["B3"].Value = TemplateStrings.Category_MoS;
        ws.Cells["B4"].Value = TemplateStrings.Category_CMoS;
        ws.Cells["B5"].Value = TemplateStrings.Category_FirstAdult;
        ws.Cells["B6"].Value = TemplateStrings.Category_SecondAdult;
        ws.Cells["B7"].Value = TemplateStrings.Category_ThirdAdult;
        ws.Cells["B8"].Value = TemplateStrings.Category_FirstJunior;
        ws.Cells["B9"].Value = TemplateStrings.Category_SecondJunior;
        ws.Cells["B10"].Value = TemplateStrings.Category_NoCategory;
        ws.Cells["C2"].Value = 50;
        ws.Cells["C3"].Value = 100;
        ws.Cells["C4"].Value = 200;
        ws.Cells["C5"].Value = 400;
        ws.Cells["C6"].Value = 800;
        ws.Cells["C7"].Value = 1500;
        ws.Cells["D2"].Value = TemplateStrings.Stroke_Fly;
        ws.Cells["D3"].Value = TemplateStrings.Stroke_Back;
        ws.Cells["D4"].Value = TemplateStrings.Stroke_Breast;
        ws.Cells["D5"].Value = TemplateStrings.Stroke_Free;
        ws.Cells["D6"].Value = TemplateStrings.Stroke_Medley;
        ApplySettingsSheetStyles(ws);
    }

    private static void ApplyValidations(ExcelWorksheet entriesSheet)
    {
        var lastSwimStyleColumn = ExcelMaxColumns;
        var distanceAddress = ExcelCellBase.GetAddress(2, FirstSwimStyleColumn, 2, lastSwimStyleColumn);
        var distanceDv = entriesSheet.DataValidations.AddListValidation(distanceAddress);
        distanceDv.Formula.ExcelFormula = $"{TemplateStrings.Sheet_Settings}!$C$2:$C$7";
        distanceDv.AllowBlank = true;
        var strokeAddress = ExcelCellBase.GetAddress(3, FirstSwimStyleColumn, 3, lastSwimStyleColumn);
        var strokeDv = entriesSheet.DataValidations.AddListValidation(strokeAddress);
        strokeDv.Formula.ExcelFormula = $"{TemplateStrings.Sheet_Settings}!$D$2:$D$6";
        strokeDv.AllowBlank = true;
        var lastEntryRow = ExcelMaxRows;
        var genderAddress = ExcelCellBase.GetAddress(FirstEntryRow, 4, lastEntryRow, 4);
        var genderDv = entriesSheet.DataValidations.AddListValidation(genderAddress);
        genderDv.Formula.ExcelFormula = $"{TemplateStrings.Sheet_Settings}!$A$2:$A$3";
        genderDv.AllowBlank = true;
        var categoryAddress = ExcelCellBase.GetAddress(FirstEntryRow, 5, lastEntryRow, 5);
        var categoryDv = entriesSheet.DataValidations.AddListValidation(categoryAddress);
        categoryDv.Formula.ExcelFormula = $"{TemplateStrings.Sheet_Settings}!$B$2:$B$10";
        categoryDv.AllowBlank = true;
    }

    private static void ApplySettingsSheetStyles(ExcelWorksheet ws)
    {
        using var header = ws.Cells["A1:E1"];
        header.Style.Font.Bold = true;
        header.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        header.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        header.Style.WrapText = true;
    }
}
