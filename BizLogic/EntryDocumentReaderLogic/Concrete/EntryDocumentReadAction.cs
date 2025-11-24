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

public partial class EntryDocumentReadAction(IEntryDocumentReaderDbAccess dbAccess) : BizActionErrors, IEntryDocumentReadAction
{
    public readonly IEntryDocumentReaderDbAccess _dbAccess = dbAccess;

    private const string FIRSTNAME_HEADER = "Фамилия";
    private const string LASTNAME_HEADER = "Имя";
    private const string BIRTH_YEAR_HEADER = "Год рождения";
    private const string SEX_HEADER = "Пол";
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

    public IReadOnlyList<EntryDocument> Action(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"File not found: {filePath}");
        using var package = new ExcelPackage(filePath);
        var workBook = package.Workbook;
        return workBook.Worksheets.Select(ReadClubEntry).ToList();
    }

    private EntryDocument ReadClubEntry(ExcelWorksheet workSheet)
    {
        _warnings = new List<string>();
        _errors = new List<string>();
        var athleteHeaders = FindHeaders(workSheet, FIRSTNAME_HEADER, LASTNAME_HEADER, BIRTH_YEAR_HEADER, SEX_HEADER, CATEGORY_HEADER);
        var clubHeaders = FindHeaders(workSheet, CLUB_NAME_HEADER, CLUB_SHORT_NAME_HEADER);
        var swimStyleHeaders = FindSwimStyles(workSheet);
        _warnings.AddRange(CheckHeaders(athleteHeaders, CATEGORY_HEADER));
        _warnings.AddRange(CheckHeaders(clubHeaders, CLUB_NAME_HEADER, CLUB_SHORT_NAME_HEADER));
        _errors.AddRange(CheckHeaders(athleteHeaders, FIRSTNAME_HEADER, LASTNAME_HEADER, BIRTH_YEAR_HEADER, SEX_HEADER));
        if (_errors.Any()) return EntryDocument.OfError(_warnings, _errors);
        var athletes = ReadAthletes(workSheet, athleteHeaders, swimStyleHeaders);
        var club = ReadClub(workSheet, clubHeaders);
        if (club == null)
        {
            _dbAccess.AddAthleteRange(athletes);
            return EntryDocument.OfAthletes(athletes, _warnings, _errors);
        }
        club.Athletes = athletes;
        _dbAccess.AddClub(club);
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
            if (!int.TryParse(workSheet.Cells[distanceRow, col].Text, out var distance))
            {
                _errors.Add($"Cannot convert distance value of the swim style in row {distanceRow}, column {col}");
                continue;
            }

            if (!EnumHelper.TryGetEnumByDescription<Stroke>(workSheet.Cells[strokeRow, col].Text, out var style))
            {
                _errors.Add($"Cannot convert stroke value of the swim style in row {strokeRow}, column {col}");
                continue;
            }

            var swimStyle = _dbAccess.GetOrAddIndividualSwimStyleByParameters(distance, style);
            swimStyles.Add(col, swimStyle);
        }

        return swimStyles;
    }

    private static List<string> CheckHeaders(Dictionary<string, ExcelCellAddress?> foundHeads, params string[] headers)
    {
        List<string> errors = [];
        foreach (var header in headers)
        {
            if (foundHeads.TryGetValue(header, out var cell) && cell is null)
            {
                errors.Add($"Header with name {header} not found");
            }
        }

        return errors;
    }

    private List<Athlete> ReadAthletes(ExcelWorksheet workSheet, Dictionary<string, ExcelCellAddress?> athleteHeaders, Dictionary<int, SwimStyle> swimStyleHeaders)
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
                _errors.Add($"Invalid athlete first name in row {row}");
                hasErrors = true;
            }

            var lastName = workSheet.Cells[row, athleteHeaders[LASTNAME_HEADER]!.Column].Text;
            if (string.IsNullOrWhiteSpace(lastName))
            {
                _errors.Add($"Invalid athlete last name in row {row}");
                hasErrors = true;
            }

            if (!int.TryParse(workSheet.Cells[row, athleteHeaders[BIRTH_YEAR_HEADER]!.Column].Text, out var yearOfBirth))
            {
                _errors.Add($"Invalid athlete year of birth in row {row}");
                hasErrors = true;
            }

            if (!EnumHelper.TryGetEnumByDescription<Gender>(workSheet.Cells[row, athleteHeaders[SEX_HEADER]!.Column].Text, out var gender))
            {
                _errors.Add($"Invalid athlete gender in row {row}");
                hasErrors = true;
            }

            if (!EnumHelper.TryGetEnumByDescription<Category>(workSheet.Cells[row, athleteHeaders[CATEGORY_HEADER]!.Column]!.Text, out var category))
            {
                _warnings.Add($"Invalid athlete category in row {row}");
                category = Category.NoCategory;
            }

            if (hasErrors) continue;

            var athlete = new Athlete()
            {
                FirstName = firstName,
                LastName = lastName,
                YearOfBirth = yearOfBirth,
                Gender = gender,
                Category = category,
            };

            var entryCols = swimStyleHeaders.Keys.Where(col => workSheet.Cells[row, col].Value != null &&
                                                               !string.IsNullOrWhiteSpace(workSheet.Cells[row, col].Text));
            var entries = new List<Entry>();
            foreach (var col in entryCols)
            {
                var entry = new Entry
                {
                    Athlete = athlete,
                    SwimStyle = swimStyleHeaders[col],
                    Scoring = workSheet.Cells[row, col].Style.Fill.PatternType == ExcelFillStyle.None,
                };
                if (EntryTimeRegex().IsMatch(workSheet.Cells[row, col].Text))
                    entry.EntryTime = ConvertEntryTimeToHundreds(workSheet.Cells[row, col].Text);
                entries.Add(entry);
            }
            athlete.Entries = entries;
            athletes.Add(athlete);
        }

        return athletes;
    }

    private Club? ReadClub(ExcelWorksheet workSheet, Dictionary<string, ExcelCellAddress?> clubHeaders)
    {
        if (!clubHeaders.TryGetValue(CLUB_NAME_HEADER, out ExcelCellAddress? colClub)) return null;
        var fromRowClub = clubHeaders.First(pair => pair.Key == CLUB_NAME_HEADER).Value!.Row + 1;
        while (workSheet.Cells[fromRowClub, colClub!.Column].IsEmpty()) fromRowClub++;
        var clubName = workSheet.Cells[fromRowClub, colClub!.Column].Text;
        if (string.IsNullOrWhiteSpace(clubName))
        {
            _warnings.Add($"Club name is empty, athletes will be added without club");
            return null;
        }

        var club = new Club
        {
            Name = clubName,
            ShortName = clubHeaders.TryGetValue(CLUB_SHORT_NAME_HEADER, out var cell) ? workSheet.Cells[fromRowClub, cell!.Column].Text : clubName,
        };
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
}