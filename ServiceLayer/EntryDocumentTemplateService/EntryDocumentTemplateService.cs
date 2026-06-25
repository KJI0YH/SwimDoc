using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Style;
using ServiceLayer.Logging;
using ServiceLayer.Resources;

namespace ServiceLayer.EntryDocumentTemplateService;

public sealed class EntryDocumentTemplateService(IAppLog log) : IEntryDocumentTemplateService
{
    private const int FirstSwimStyleColumn = 5;
    private const int LastSwimStyleColumnInTemplate = 22;
    private const double SwimStyleColumnWidth = 8;
    private const int FirstEntryRow = 4;
    private const int LastEntryGridRow = 200;
    private const int ExcelMaxColumns = 16384;
    private const int ExcelMaxRows = 1048576;
    private const double ThemeLightTint60 = 0.6;
    public byte[] CreateTemplate()
    {
        using var package = new ExcelPackage();
        var entriesSheet = package.Workbook.Worksheets.Add(TemplateStrings.Sheet_Entries);
        BuildEntriesSheet(entriesSheet);
        var settingsSheet = package.Workbook.Worksheets.Add(TemplateStrings.Sheet_Settings);
        BuildSettingsSheet(settingsSheet);
        ApplyValidations(entriesSheet);
        log.Info("Created entry document template (Excel)");
        return package.GetAsByteArray();
    }

    private static void BuildEntriesSheet(ExcelWorksheet ws)
    {
        ws.Cells.Style.Font.Name = "Calibri";
        ws.Cells.Style.Font.Size = 11;
        ws.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        ws.Cells.Style.WrapText = true;
        ws.Column(1).Width = 35;
        ws.Column(2).Width = 10;
        ws.Column(3).Width = 10;
        ws.Column(4).Width = 8;
        for (var col = FirstSwimStyleColumn; col <= LastSwimStyleColumnInTemplate; col++)
            ws.Column(col).Width = SwimStyleColumnWidth;
        ws.Row(3).Height = 15;
        ws.Cells["A1"].Value = TemplateStrings.Entries_Title_A1;
        ws.Cells["B1"].Value = TemplateStrings.Entries_Title_B1;
        ws.Cells["A2"].Value = TemplateStrings.Entries_Header_FullName;
        ws.Cells["B2"].Value = TemplateStrings.Entries_Header_BirthYear;
        ws.Cells["C2"].Value = TemplateStrings.Entries_Header_Gender;
        ws.Cells["D2"].Value = TemplateStrings.Entries_Header_Category;
        ws.Cells["E2"].Value = TemplateStrings.Stroke_Fly;
        ws.Cells["H2"].Value = TemplateStrings.Stroke_Back;
        ws.Cells["K2"].Value = TemplateStrings.Stroke_Breast;
        ws.Cells["N2"].Value = TemplateStrings.Stroke_Free;
        ws.Cells["T2"].Value = TemplateStrings.Stroke_Medley;

        ws.Cells["E3"].Value = 50;
        ws.Cells["F3"].Value = 100;
        ws.Cells["G3"].Value = 200;
        ws.Cells["H3"].Value = 50;
        ws.Cells["I3"].Value = 100;
        ws.Cells["J3"].Value = 200;
        ws.Cells["K3"].Value = 50;
        ws.Cells["L3"].Value = 100;
        ws.Cells["M3"].Value = 200;
        ws.Cells["N3"].Value = 50;
        ws.Cells["O3"].Value = 100;
        ws.Cells["P3"].Value = 200;
        ws.Cells["Q3"].Value = 400;
        ws.Cells["R3"].Value = 800;
        ws.Cells["S3"].Value = 1500;
        ws.Cells["T3"].Value = 100;
        ws.Cells["U3"].Value = 200;
        ws.Cells["V3"].Value = 400;
        
        ws.Cells["A4"].Value = TemplateStrings.Entries_Example_FullName;
        ws.Cells["B4"].Value = 2003;
        ws.Cells["C4"].Value = TemplateStrings.Gender_Male;
        ws.Cells["D4"].Value = TemplateStrings.Entries_Example_Category;
        ws.Cells["H4"].Style.Numberformat.Format = "@";
        ws.Cells["H4"].Value = "27.55";
        ws.Cells["I4"].Style.Numberformat.Format = "@";
        ws.Cells["I4"].Value = "59.02";
        ws.Cells["N4"].Style.Numberformat.Format = "@";
        ws.Cells["N4"].Value = "24.37";
        ws.Cells["O4"].Style.Numberformat.Format = "@";
        ws.Cells["O4"].Value = "54.07";        
        ws.Cells["T4"].Style.Numberformat.Format = "@";
        ws.Cells["T4"].Value = "59.99";
        
        ws.Cells["B1:V1"].Merge = true;
        ws.Cells["A2:A3"].Merge = true;
        ws.Cells["B2:B3"].Merge = true;
        ws.Cells["C2:C3"].Merge = true;
        ws.Cells["D2:D3"].Merge = true;
        ws.Cells["E2:G2"].Merge = true;
        ws.Cells["H2:J2"].Merge = true;
        ws.Cells["K2:M2"].Merge = true;
        ws.Cells["N2:S2"].Merge = true;
        ws.Cells["T2:V2"].Merge = true;
        ApplyEntriesSheetStyles(ws);
    }

    private static void ApplyEntriesSheetStyles(ExcelWorksheet ws)
    {
        using (var clubLabel = ws.Cells["A1"])
        {
            clubLabel.Style.Font.Bold = true;
            clubLabel.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            clubLabel.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            clubLabel.Style.WrapText = true;
            ApplyThemeFill(clubLabel, eThemeSchemeColor.Accent2, ThemeLightTint60);
            ApplyThinBorder(clubLabel);
        }

        using (var clubName = ws.Cells["B1"])
        {
            clubName.Style.Font.Bold = false;
            clubName.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            clubName.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            clubName.Style.WrapText = true;
            ApplyThemeFill(clubName, eThemeSchemeColor.Accent4, ThemeLightTint60);
            ApplyThinBorder(clubName);
        }

        var headerAddress = $"A2:{ExcelCellBase.GetAddress(3, LastSwimStyleColumnInTemplate)}";
        using (var columnHeaders = ws.Cells[headerAddress])
        {
            columnHeaders.Style.Font.Bold = true;
            columnHeaders.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            columnHeaders.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            columnHeaders.Style.WrapText = true;
            ApplyThemeFill(columnHeaders, eThemeSchemeColor.Accent5, ThemeLightTint60);
        }

        ws.Row(2).Style.Font.Bold = true;
        ws.Row(3).Style.Font.Bold = true;

        var gridAddress = ExcelCellBase.GetAddress(2, 1, LastEntryGridRow, LastSwimStyleColumnInTemplate);
        using (var entriesGrid = ws.Cells[gridAddress])
            ApplyThinBorder(entriesGrid);

        ws.View.ShowGridLines = false;
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
        var strokeAddress = ExcelCellBase.GetAddress(2, FirstSwimStyleColumn, 2, lastSwimStyleColumn);
        var strokeDv = entriesSheet.DataValidations.AddListValidation(strokeAddress);
        strokeDv.Formula.ExcelFormula = $"{TemplateStrings.Sheet_Settings}!$D$2:$D$6";
        strokeDv.AllowBlank = true;
        var distanceAddress = ExcelCellBase.GetAddress(3, FirstSwimStyleColumn, 3, lastSwimStyleColumn);
        var distanceDv = entriesSheet.DataValidations.AddListValidation(distanceAddress);
        distanceDv.Formula.ExcelFormula = $"{TemplateStrings.Sheet_Settings}!$C$2:$C$7";
        distanceDv.AllowBlank = true;
        var lastEntryRow = ExcelMaxRows;
        var genderAddress = ExcelCellBase.GetAddress(FirstEntryRow, 3, lastEntryRow, 3);
        var genderDv = entriesSheet.DataValidations.AddListValidation(genderAddress);
        genderDv.Formula.ExcelFormula = $"{TemplateStrings.Sheet_Settings}!$A$2:$A$3";
        genderDv.AllowBlank = true;
        var categoryAddress = ExcelCellBase.GetAddress(FirstEntryRow, 4, lastEntryRow, 4);
        var categoryDv = entriesSheet.DataValidations.AddListValidation(categoryAddress);
        categoryDv.Formula.ExcelFormula = $"{TemplateStrings.Sheet_Settings}!$B$2:$B$10";
        categoryDv.AllowBlank = true;
    }

    private static void ApplySettingsSheetStyles(ExcelWorksheet ws)
    {
        using var header = ws.Cells["A1:D1"];
        header.Style.Font.Bold = true;
        header.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        header.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        header.Style.WrapText = true;
        ApplyHeaderFill(header);
        ApplyThinBorder(header);
    }

    private static void ApplyHeaderFill(ExcelRange range) =>
        ApplyThemeFill(range, eThemeSchemeColor.Accent5, ThemeLightTint60);

    private static void ApplyThemeFill(ExcelRange range, eThemeSchemeColor theme, double tint)
    {
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.SetBackground(theme);
        range.Style.Fill.BackgroundColor.Tint = tint;
    }

    private static void ApplyThinBorder(ExcelRange range)
    {
        var border = range.Style.Border;
        border.Top.Style = ExcelBorderStyle.Thin;
        border.Bottom.Style = ExcelBorderStyle.Thin;
        border.Left.Style = ExcelBorderStyle.Thin;
        border.Right.Style = ExcelBorderStyle.Thin;
    }
}
