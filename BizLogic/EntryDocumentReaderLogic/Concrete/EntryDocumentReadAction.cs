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
    private static readonly string[] SettingsWorksheetNames = ["Настройки", "Settings"];

    private const string FIRSTNAME_HEADER = "FirstName";
    private const string LASTNAME_HEADER = "LastName";
    private const string BIRTH_YEAR_HEADER = "BirthYear";
    private const string GENDER_HEADER = "Gender";
    private const string CATEGORY_HEADER = "Category";

    private const string CLUB_NAME_HEADER = "ClubName";

    private static readonly string[] ButterflyNames = ["Баттерфляй", "Butterfly"];
    private static readonly string[] BackstrokeNames = ["На спине", "Backstroke"];
    private static readonly string[] BreaststrokeNames = ["Брасс", "Breaststroke"];
    private static readonly string[] FreestyleNames = ["Вольный стиль", "Freestyle"];
    private static readonly string[] MedleyNames = ["Комплексное плавание", "Medley"];

    private static readonly Dictionary<string, string[]> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        [FIRSTNAME_HEADER] = ["Имя", "First name", "First Name", "Firstname"],
        [LASTNAME_HEADER] = ["Фамилия", "Last name", "Last Name", "Lastname"],
        [BIRTH_YEAR_HEADER] = ["Год рождения", "Birth year", "Birth Year", "Year of birth", "Year Of Birth"],
        [GENDER_HEADER] = ["Пол", "Gender"],
        [CATEGORY_HEADER] = ["Разряд", "Category"],
        [CLUB_NAME_HEADER] = ["Команда", "Team", "Club", "Team name", "Club name"]
    };

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
                !SettingsWorksheetNames.Contains(worksheet.Name?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            .Select(ReadClubEntry).ToList();
    }

    private EntryDocument ReadClubEntry(ExcelWorksheet workSheet)
    {
        _warnings = [];
        _errors = [];
        var athleteHeaders = FindHeaders(workSheet, FIRSTNAME_HEADER, LASTNAME_HEADER, BIRTH_YEAR_HEADER, GENDER_HEADER, CATEGORY_HEADER);
        var clubHeaders = FindHeaders(workSheet, CLUB_NAME_HEADER);
        var swimStyleHeaders = FindSwimStyles(workSheet);
        _warnings.AddRange(CheckHeaders(workSheet, athleteHeaders, CATEGORY_HEADER));
        _warnings.AddRange(CheckHeaders(workSheet, clubHeaders, CLUB_NAME_HEADER));
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

    private static Dictionary<string, ExcelCellAddress?> FindHeaders(ExcelWorksheet workSheet, params string[] canonicalHeaders)
    {
        var headersCols = canonicalHeaders.ToDictionary<string, string, ExcelCellAddress?>(k => k, v => null);
        var usedCells = workSheet.Cells.Where(cell => !string.IsNullOrEmpty(cell.Text));
        foreach (var cell in usedCells)
        {
            if (!headersCols.ContainsValue(null)) break;
            foreach (var canonical in canonicalHeaders)
            {
                var aliases = HeaderAliases.TryGetValue(canonical, out var list) ? list : [canonical];
                if (aliases.Contains(cell.Text, StringComparer.OrdinalIgnoreCase))
                {
                    headersCols[canonical] = cell.Start;
                    break;
                }
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
        var strokeHeaders = ButterflyNames
            .Concat(BackstrokeNames)
            .Concat(BreaststrokeNames)
            .Concat(FreestyleNames)
            .Concat(MedleyNames)
            .ToArray();
        var startCell = FindFirstHeaderFromList(workSheet, strokeHeaders);
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
            if (!TryParseStroke(strokeText, out var style))
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

    private static bool TryParseStroke(string text, out Stroke stroke)
    {
        var t = (text ?? string.Empty).Trim();
        if (ButterflyNames.Contains(t, StringComparer.OrdinalIgnoreCase)) { stroke = Stroke.Fly; return true; }
        if (BackstrokeNames.Contains(t, StringComparer.OrdinalIgnoreCase)) { stroke = Stroke.Back; return true; }
        if (BreaststrokeNames.Contains(t, StringComparer.OrdinalIgnoreCase)) { stroke = Stroke.Breast; return true; }
        if (FreestyleNames.Contains(t, StringComparer.OrdinalIgnoreCase)) { stroke = Stroke.Free; return true; }
        if (MedleyNames.Contains(t, StringComparer.OrdinalIgnoreCase)) { stroke = Stroke.Medley; return true; }
        stroke = default;
        return false;
    }

    private static bool TryParseGender(string text, out Gender gender)
    {
        var t = (text ?? string.Empty).Trim();
        if (string.Equals(t, "Мужчины", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "Men", StringComparison.OrdinalIgnoreCase))
        { gender = Gender.Male; return true; }
        if (string.Equals(t, "Женщины", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "Women", StringComparison.OrdinalIgnoreCase))
        { gender = Gender.Female; return true; }
        if (string.Equals(t, "Смешанная", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "Mixed", StringComparison.OrdinalIgnoreCase))
        { gender = Gender.Mixed; return true; }
        gender = default;
        return false;
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

            if (!TryParseGender(workSheet.Cells[row, athleteHeaders[GENDER_HEADER]!.Column].Text, out var gender))
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
        var row = clubHeaders.First(pair => pair.Key == CLUB_NAME_HEADER).Value!.Row;
        var col = colClub!.Column;
        // Support both formats: either club name in the header cell itself, or in the next column.
        var clubName = workSheet.Cells[row, col].Text;
        if (string.Equals(clubName?.Trim(), HeaderAliases[CLUB_NAME_HEADER].First(), StringComparison.OrdinalIgnoreCase) ||
            HeaderAliases[CLUB_NAME_HEADER].Contains(clubName ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            while (workSheet.Cells[row, col + 1].IsEmpty()) row++;
            clubName = workSheet.Cells[row, col + 1].Text;
        }
        if (string.IsNullOrWhiteSpace(clubName))
        {
            _warnings.Add($"Имя клуба не найдено, спортсмены добавлены в личный зачёт: {MessageLocation(workSheet.Name, null, null)}");
            return null;
        }

        return dbAccess.GetOrAddClub(clubName);
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