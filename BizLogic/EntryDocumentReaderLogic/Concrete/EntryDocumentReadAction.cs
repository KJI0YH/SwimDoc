using System.Diagnostics.Contracts;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using BizDbAccess;
using BizLogic.GenericInterfaces;
using BizLogic.Helpers;
using DataLayer.EfClasses;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BizLogic.EntryDocumentReaderLogic.Concrete;

public partial class EntryDocumentReadAction(IEntryDocumentReaderDbAccess dbAccess)
    : BizActionErrors, IEntryDocumentReadAction
{
    private const string SETTINGS_WORKSHEET_NAME = "Настройки";
    private const string FIRSTNAME_HEADER = "Имя";
    private const string LASTNAME_HEADER = "Фамилия";
    private const string BIRTH_YEAR_HEADER = "Год рождения";
    private const string GENDER_HEADER = "Пол";
    private const string CATEGORY_HEADER = "Разряд";

    private const string CLUB_NAME_HEADER = "Команда";
    private const string CLUB_SHORT_NAME_HEADER = "Короткое название";

    private const string BUTTERFLY = "Баттерфляй";
    private const string BACKSTROKE = "На спине";
    private const string BREASTROKE = "Брасс";
    private const string FREESTYLE = "Вольный стиль";
    private const string MEDLEY = "Комплексное плавание";

    private List<string> _warnings;
    private List<string> _errors;

    [GeneratedRegex(@"(?:\d{1,}\D)?[0-5]\d\D\d{2}")]
    private static partial Regex EntryTimeRegex();

    public IReadOnlyList<EntryDocument> Action(string dataIn)
    {
        if (!File.Exists(dataIn)) throw new FileNotFoundException($"File not found: {dataIn}");
        using var package = new ExcelPackage(dataIn);
        var workBook = package.Workbook;
        return workBook.Worksheets
            .Where(worksheet =>
                !string.Equals(worksheet.Name?.Trim(), SETTINGS_WORKSHEET_NAME, StringComparison.OrdinalIgnoreCase))
            .Select(ReadClubEntry).ToList();
    }

    private EntryDocument ReadClubEntry(ExcelWorksheet workSheet)
    {
        _warnings = [];
        _errors = [];
        var athleteHeaders = FindHeaders(workSheet, FIRSTNAME_HEADER, LASTNAME_HEADER, BIRTH_YEAR_HEADER, GENDER_HEADER,
            CATEGORY_HEADER);
        var clubHeaders = FindHeaders(workSheet, CLUB_NAME_HEADER, CLUB_SHORT_NAME_HEADER);
        var swimStyleHeaders = FindSwimStyles(workSheet);
        _warnings.AddRange(CheckHeaders(workSheet, athleteHeaders, CATEGORY_HEADER));
        _warnings.AddRange(CheckHeaders(workSheet, clubHeaders, CLUB_NAME_HEADER, CLUB_SHORT_NAME_HEADER));
        _errors.AddRange(CheckHeaders(workSheet, athleteHeaders, FIRSTNAME_HEADER, LASTNAME_HEADER, BIRTH_YEAR_HEADER,
            GENDER_HEADER));
        if (_errors.Count != 0) return EntryDocument.OfError(_warnings, _errors);
        var athletes = ReadAthletes(workSheet, athleteHeaders, swimStyleHeaders);
        var club = ReadClub(workSheet, clubHeaders);
        if (club is null)
        {
            return EntryDocument.OfAthletes(athletes, _warnings, _errors);
        }

        club.Athletes = athletes;
        return EntryDocument.OfClub(club, _warnings, _errors);
    }

    private static Dictionary<string, ExcelCellAddress?> FindHeaders(ExcelWorksheet workSheet, params string[] headers)
    {
        var headersCols = headers.ToDictionary<string, string, ExcelCellAddress?>(k => k, v => null);
        var usedCells = workSheet.Cells.Where(cell => !string.IsNullOrEmpty(cell.Text));
        foreach (var cell in usedCells)
        {
            if (!headersCols.ContainsValue(null)) break;
            if (headers.Contains(cell.Text, StringComparer.OrdinalIgnoreCase))
            {
                headersCols[cell.Text] = cell.Start;
            }
        }

        return headersCols;
    }

    private static ExcelCellAddress? FindFirstHeaderFromList(ExcelWorksheet workSheet, params string[] headers)
    {
        var usedCells = workSheet.Cells.Where(cell => !string.IsNullOrEmpty(cell.Text));
        foreach (var cell in usedCells)
        {
            if (headers.Contains(cell.Text, StringComparer.OrdinalIgnoreCase))
                return cell.Start;
        }

        return null;
    }

    private Dictionary<int, SwimStyle> FindSwimStyles(ExcelWorksheet workSheet)
    {
        Dictionary<int, SwimStyle> swimStyles = new();
        var startCell = FindFirstHeaderFromList(workSheet, BUTTERFLY, BACKSTROKE, BREASTROKE, FREESTYLE, MEDLEY);
        if (startCell is null) return swimStyles;
        var startCol = startCell.Column;
        var endCol = workSheet.Dimension.End.Column;
        var distanceRow = startCell.Row - 1;
        var strokeRow = startCell.Row;
        for (var col = startCol; col <= endCol; col++)
        {
            var distanceText = workSheet.Cells[distanceRow, col].Text;
            if (string.IsNullOrWhiteSpace(distanceText)) continue;
            if (!int.TryParse(distanceText, out var distance))
            {
                _warnings.Add($"Не удалось обработать дистанцию: {MessageLocation(workSheet.Name, distanceRow, col)}");
                continue;
            }

            var strokeText = workSheet.Cells[strokeRow, col].Text;
            if (string.IsNullOrWhiteSpace(strokeText)) continue;
            if (!EnumHelper.TryGetEnumByDescription<Stroke>(workSheet.Cells[strokeRow, col].Text, out var style))
            {
                _warnings.Add($"Не удалось обработать стиль: {MessageLocation(workSheet.Name, strokeRow, col)}");
                continue;
            }

            var swimStyle = dbAccess.GetOrAddIndividualSwimStyleByParameters(distance, style);
            swimStyles.Add(col, swimStyle);
        }

        return swimStyles;
    }

    private List<string> CheckHeaders(ExcelWorksheet workSheet, Dictionary<string, ExcelCellAddress?> foundHeads,
        params string[] headers)
    {
        List<string> errors = [];
        foreach (var header in headers)
        {
            if (foundHeads.TryGetValue(header, out var cell) && cell is null)
            {
                errors.Add(
                    $"Заголовок \"{header}\" не найден: {MessageLocation(workSheet.Name, cell?.Row, cell?.Column)}");
            }
        }

        return errors;
    }

    private List<Athlete> ReadAthletes(ExcelWorksheet workSheet, Dictionary<string, ExcelCellAddress?> athleteHeaders,
        Dictionary<int, SwimStyle> swimStyleHeaders)
    {
        List<Athlete> athletes = [];
        var fromRowAthlete = athleteHeaders.First(pair => pair.Key == FIRSTNAME_HEADER).Value!.Row + 1;
        while (workSheet.Cells[fromRowAthlete, athleteHeaders[FIRSTNAME_HEADER]!.Column].IsEmpty()) fromRowAthlete++;
        var toRowAthlete = workSheet.Dimension.End.Row;
        for (var row = fromRowAthlete; row <= toRowAthlete; row++)
        {
            var hasErrors = false;
            var firstName = workSheet.Cells[row, athleteHeaders[FIRSTNAME_HEADER]!.Column].Text;
            if (string.IsNullOrWhiteSpace(firstName))
            {
                _errors.Add(
                    $"Некорректное имя спортсмена: {MessageLocation(workSheet.Name, row, athleteHeaders[FIRSTNAME_HEADER]!.Column)}");
                hasErrors = true;
            }

            var lastName = workSheet.Cells[row, athleteHeaders[LASTNAME_HEADER]!.Column].Text;
            if (string.IsNullOrWhiteSpace(lastName))
            {
                _errors.Add(
                    $"Некорректная фамилия спортсмена: {MessageLocation(workSheet.Name, row, athleteHeaders[LASTNAME_HEADER]!.Column)}");
                hasErrors = true;
            }

            if (!int.TryParse(workSheet.Cells[row, athleteHeaders[BIRTH_YEAR_HEADER]!.Column].Text,
                    out var yearOfBirth))
            {
                _errors.Add(
                    $"Некорректный год рождения спортсмена: {MessageLocation(workSheet.Name, row, athleteHeaders[BIRTH_YEAR_HEADER]!.Column)}");
                hasErrors = true;
            }

            if (!EnumHelper.TryGetEnumByDescription<Gender>(
                    workSheet.Cells[row, athleteHeaders[GENDER_HEADER]!.Column].Text, out var gender))
            {
                _errors.Add(
                    $"Некорректный пол спортсмена: {MessageLocation(workSheet.Name, row, athleteHeaders[GENDER_HEADER]!.Column)}");
                hasErrors = true;
            }

            if (!EnumHelper.TryGetEnumByDescription<Category>(
                    workSheet.Cells[row, athleteHeaders[CATEGORY_HEADER]!.Column]!.Text, out var category))
            {
                _errors.Add(
                    $"Некорректный разряд спортсмена: {MessageLocation(workSheet.Name, row, athleteHeaders[CATEGORY_HEADER]!.Column)}");
                category = Category.NoCategory;
            }

            if (hasErrors) continue;

            var athlete = dbAccess.GetOrAddAthlete(firstName, lastName, yearOfBirth, gender, category);

            var entryCols = swimStyleHeaders.Keys.Where(col => workSheet.Cells[row, col].Value != null &&
                                                               !string.IsNullOrWhiteSpace(
                                                                   workSheet.Cells[row, col].Text));
            var entries = new List<Entry>();
            foreach (var col in entryCols)
            {
                var entry = dbAccess.GetOrAddEntry(athlete, swimStyleHeaders[col],
                    workSheet.Cells[row, col].Style.Fill.PatternType == ExcelFillStyle.None,
                    EntryTimeRegex().IsMatch(workSheet.Cells[row, col].Text)
                        ? ConvertEntryTimeToHundreds(workSheet.Cells[row, col].Text)
                        : null);
                entries.Add(entry);
            }

            athlete.Entries = entries;
            athletes.Add(athlete);
        }

        return athletes;
    }

    private Club? ReadClub(ExcelWorksheet workSheet, Dictionary<string, ExcelCellAddress?> clubHeaders)
    {
        if (!clubHeaders.TryGetValue(CLUB_NAME_HEADER, out var colClub)) return null;
        var fromRowClub = clubHeaders.First(pair => pair.Key == CLUB_NAME_HEADER).Value!.Row + 1;
        while (workSheet.Cells[fromRowClub, colClub!.Column].IsEmpty()) fromRowClub++;
        var clubName = workSheet.Cells[fromRowClub, colClub!.Column].Text;
        if (string.IsNullOrWhiteSpace(clubName))
        {
            _warnings.Add(
                $"Имя клуба не найдено, спортсмены добавлены в личный зачёт: {MessageLocation(workSheet.Name, null, null)}");
            return null;
        }

        var shortName = clubHeaders.TryGetValue(CLUB_SHORT_NAME_HEADER, out var cell)
            ? workSheet.Cells[fromRowClub, cell!.Column].Text
            : clubName;
        if (string.IsNullOrWhiteSpace(shortName))
            shortName = clubName;
        var club = dbAccess.GetOrAddClub(clubName, shortName);
        return club;
    }

    private static int ConvertEntryTimeToHundreds(string entryTime)
    {
        if (string.IsNullOrWhiteSpace(entryTime)) return 0;
        var matches = Regex.Matches(entryTime, @"\d+");
        var hundreds = int.Parse(matches.Last().Value);
        var seconds = int.Parse(matches[matches.Count > 2 ? 1 : 0].Value);
        var minutes = matches.Count > 2 ? int.Parse(matches[0].Value) : 0;
        return minutes * 60 * 100 + seconds * 100 + hundreds;
    }

    private string MessageLocation(string worksheetName, int? row, int? col)
    {
        return $"'{worksheetName}'[{row ?? '?'}:{col ?? '?'}]";
    }
}