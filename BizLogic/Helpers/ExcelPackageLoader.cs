using NPOI.SS.UserModel;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BizLogic.Helpers;

public static class ExcelPackageLoader
{
    private static readonly byte[] OleCompoundHeader = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public static ExcelPackage Open(string filePath)
    {
        if (IsLegacyXls(filePath))
            return OpenLegacyWorkbook(filePath);

        return new ExcelPackage(new FileInfo(filePath));
    }

    private static bool IsLegacyXls(string filePath)
    {
        if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
            filePath.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase))
            return false;

        if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            return true;

        using var stream = File.OpenRead(filePath);
        Span<byte> header = stackalloc byte[OleCompoundHeader.Length];
        return stream.Read(header) == header.Length && header.SequenceEqual(OleCompoundHeader);
    }

    private static ExcelPackage OpenLegacyWorkbook(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var workbook = WorkbookFactory.Create(stream);
        var package = new ExcelPackage();

        for (var sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
        {
            var sourceSheet = workbook.GetSheetAt(sheetIndex);
            var worksheet = package.Workbook.Worksheets.Add(sourceSheet.SheetName);
            CopySheet(sourceSheet, worksheet);
        }

        return package;
    }

    private static void CopySheet(ISheet sourceSheet, ExcelWorksheet destinationSheet)
    {
        for (var rowIndex = sourceSheet.FirstRowNum; rowIndex <= sourceSheet.LastRowNum; rowIndex++)
        {
            var sourceRow = sourceSheet.GetRow(rowIndex);
            if (sourceRow is null)
                continue;

            for (var columnIndex = Math.Max(0, (int)sourceRow.FirstCellNum);
                 columnIndex < sourceRow.LastCellNum;
                 columnIndex++)
            {
                var sourceCell = sourceRow.GetCell(columnIndex);
                if (sourceCell is null)
                    continue;

                var destinationCell = destinationSheet.Cells[rowIndex + 1, columnIndex + 1];
                destinationCell.Value = GetCellValue(sourceCell);
                ApplyFill(sourceCell, destinationCell);
            }
        }
    }

    private static object? GetCellValue(ICell cell)
    {
        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue,
            CellType.Numeric => DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue : cell.NumericCellValue,
            CellType.Boolean => cell.BooleanCellValue,
            CellType.Formula => GetFormulaCellValue(cell),
            CellType.Blank => null,
            _ => cell.ToString()
        };
    }

    private static object? GetFormulaCellValue(ICell cell)
    {
        return cell.CachedFormulaResultType switch
        {
            CellType.String => cell.StringCellValue,
            CellType.Numeric => DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue : cell.NumericCellValue,
            CellType.Boolean => cell.BooleanCellValue,
            _ => cell.ToString()
        };
    }

    private static void ApplyFill(ICell sourceCell, ExcelRange destinationCell)
    {
        if (IsUnfilledCell(sourceCell))
        {
            destinationCell.Style.Fill.PatternType = ExcelFillStyle.None;
            return;
        }

        destinationCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
    }

    private static bool IsUnfilledCell(ICell sourceCell)
    {
        var style = sourceCell.CellStyle;
        if (style is null)
            return true;

        return style.FillPattern switch
        {
            FillPattern.NoFill => true,
            FillPattern.SolidForeground => IsAutomaticOrWhiteFill(style),
            _ => false
        };
    }

    private static bool IsAutomaticOrWhiteFill(ICellStyle style)
    {
        if (style.FillForegroundColor == IndexedColors.Automatic.Index)
            return true;

        var index = style.FillForegroundColor;
        return index == IndexedColors.Automatic.Index ||
               index == IndexedColors.White.Index;
    }
}
