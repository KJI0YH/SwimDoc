using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using OfficeOpenXml;

namespace Tests.TestInfrastructure;

public static class EntryDocumentFileHelper
{
    public static string ConvertXlsxToXls(string xlsxPath, string outputPath)
    {
        using var stream = File.OpenRead(xlsxPath);
        using var source = WorkbookFactory.Create(stream);
        var destination = new HSSFWorkbook();

        for (var sheetIndex = 0; sheetIndex < source.NumberOfSheets; sheetIndex++)
        {
            var sourceSheet = source.GetSheetAt(sheetIndex);
            var destinationSheet = destination.CreateSheet(sourceSheet.SheetName);
            CopySheet(sourceSheet, destinationSheet, destination);
        }

        using var outputStream = File.Create(outputPath);
        destination.Write(outputStream);
        return outputPath;
    }

    public static string CreateTwoTeamWorkbook(string sourceXlsxPath, string outputPath, string secondTeamName)
    {
        using var package = new ExcelPackage(new FileInfo(sourceXlsxPath));
        var templateSheet = package.Workbook.Worksheets[0];
        var secondSheet = package.Workbook.Worksheets.Add(secondTeamName, templateSheet);
        secondSheet.Cells["B1"].Value = secondTeamName;

        for (var row = 4; row <= 6; row++)
        {
            var athleteIndex = row - 4;
            secondSheet.Cells[row, 1].Value = $"ИмяБ{athleteIndex}";
            secondSheet.Cells[row, 2].Value = $"ФамилияБ{athleteIndex}";
        }

        var lastRow = secondSheet.Dimension.End.Row;
        for (var row = 7; row <= lastRow; row++)
        {
            for (var col = 1; col <= secondSheet.Dimension.End.Column; col++)
                secondSheet.Cells[row, col].Value = null;
        }

        package.SaveAs(new FileInfo(outputPath));
        return outputPath;
    }

    private static void CopySheet(ISheet sourceSheet, ISheet destinationSheet, HSSFWorkbook destinationWorkbook)
    {
        for (var rowIndex = sourceSheet.FirstRowNum; rowIndex <= sourceSheet.LastRowNum; rowIndex++)
        {
            var sourceRow = sourceSheet.GetRow(rowIndex);
            if (sourceRow is null)
                continue;

            var destinationRow = destinationSheet.CreateRow(rowIndex);
            for (var columnIndex = Math.Max(0, (int)sourceRow.FirstCellNum);
                 columnIndex < sourceRow.LastCellNum;
                 columnIndex++)
            {
                var sourceCell = sourceRow.GetCell(columnIndex);
                if (sourceCell is null)
                    continue;

                var destinationCell = destinationRow.CreateCell(columnIndex);
                CopyCellValue(sourceCell, destinationCell, destinationWorkbook);
            }
        }
    }

    private static void CopyCellValue(ICell sourceCell, ICell destinationCell, HSSFWorkbook destinationWorkbook)
    {
        switch (sourceCell.CellType)
        {
            case CellType.String:
                destinationCell.SetCellValue(sourceCell.StringCellValue);
                break;
            case CellType.Numeric:
                destinationCell.SetCellValue(sourceCell.NumericCellValue);
                break;
            case CellType.Boolean:
                destinationCell.SetCellValue(sourceCell.BooleanCellValue);
                break;
            case CellType.Formula:
                destinationCell.SetCellValue(sourceCell.ToString());
                break;
            default:
                destinationCell.SetCellValue(sourceCell.ToString());
                break;
        }

        if (sourceCell.CellStyle.FillPattern == FillPattern.NoFill)
            return;

        var destinationStyle = destinationWorkbook.CreateCellStyle();
        destinationStyle.FillPattern = FillPattern.SolidForeground;
        destinationStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
        destinationCell.CellStyle = destinationStyle;
    }
}
