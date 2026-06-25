using System.Globalization;
using System.Text.RegularExpressions;
using BizDbAccess;
using BizDbAccess.EntryDocumentReader;
using BizLogic.GenericInterfaces;
using BizLogic.Helpers;
using BizLogic.Resources;
using DataLayer.EfClasses;
using DataLayer.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BizLogic.EntryDocumentReader.Concrete;

public partial class EntryDocumentReadAction(
    IEntryDocumentReaderDbAccess dbAccess,
    EntryImportHighlightScoringMode highlightScoringMode,
    IBizLog log) : BizActionErrors, IEntryDocumentReadAction
{
    private static readonly string[] SettingsWorksheetNames = ["Настройки", "Settings"];
    private const string FIRSTNAME_HEADER = "FirstName";
    private const string LASTNAME_HEADER = "LastName";
    private const string BIRTH_YEAR_HEADER = "BirthYear";
    private const string GENDER_HEADER = "Gender";
    private const string CATEGORY_HEADER = "Category";
    private const string CLUB_NAME_HEADER = "ClubName";
    private static readonly string[] StrokeHeaderNames =
    [
        "Баттерфляй", "Butterfly", "Fly",
        "На спине", "Backstroke", "Back",
        "Брасс", "Breaststroke", "Breast",
        "Вольный стиль", "Freestyle", "Free",
        "Комплексное плавание", "Medley"
    ];

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
    private readonly EntryImportHighlightScoringMode _highlightScoringMode = highlightScoringMode;

    [GeneratedRegex(@"^\s*(?:\d{1,}\D)?[0-5]\d(?:\D\d{1,2})?\s*$")]
    private static partial Regex EntryTimeRegex();

    public IReadOnlyList<EntryDocument> Action(string dataIn) => Action(dataIn, CancellationToken.None);

    public IReadOnlyList<EntryDocument> Action(string dataIn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(dataIn))
            throw new FileNotFoundException(string.Format(CultureInfo.CurrentUICulture,
                EntryImportStrings.FileNotFound_Format, dataIn));
        log.Info($"Parse entry document: \"{Path.GetFileName(dataIn)}\"");
        using var package = OpenEntryFile(dataIn);
        var workBook = package.Workbook;
        var worksheets = workBook.Worksheets
            .Where(worksheet =>
                !SettingsWorksheetNames.Contains(worksheet.Name?.Trim() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase))
            .ToList();
        var documents = new List<EntryDocument>(worksheets.Count);
        foreach (var worksheet in worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            documents.Add(ReadClubEntry(worksheet, cancellationToken));
        }

        log.Info($"Parse entry document finished: \"{Path.GetFileName(dataIn)}\", worksheets={documents.Count}");
        return documents;
    }

    private EntryDocument ReadClubEntry(ExcelWorksheet workSheet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        log.Info($"Parse worksheet: \"{workSheet.Name}\"");
        _warnings = [];
        _errors = [];
        var athleteHeaders = FindHeaders(workSheet, cancellationToken, FIRSTNAME_HEADER, LASTNAME_HEADER,
            BIRTH_YEAR_HEADER, GENDER_HEADER, CATEGORY_HEADER);
        var clubHeaders = FindHeaders(workSheet, cancellationToken, CLUB_NAME_HEADER);
        var swimStyleHeaders = FindSwimStyles(workSheet, cancellationToken);
        _warnings.AddRange(CheckHeaders(workSheet, clubHeaders, CLUB_NAME_HEADER));
        _errors.AddRange(CheckHeaders(workSheet, athleteHeaders, FIRSTNAME_HEADER, LASTNAME_HEADER, BIRTH_YEAR_HEADER,
            GENDER_HEADER));
        if (_errors.Count != 0)
        {
            LogWorksheetMessages(workSheet.Name);
            return EntryDocument.OfError(_warnings, _errors);
        }
        var athletes = ReadAthletes(workSheet, athleteHeaders, swimStyleHeaders, cancellationToken);
        var club = ReadClub(workSheet, clubHeaders);
        if (club is null)
        {
            LogWorksheetMessages(workSheet.Name);
            return EntryDocument.OfAthletes(athletes, _warnings, _errors);
        }

        club.Athletes = athletes;
        LogWorksheetMessages(workSheet.Name);
        return EntryDocument.OfClub(club, _warnings, _errors);
    }

    private static Dictionary<string, ExcelCellAddress?> FindHeaders(
        ExcelWorksheet workSheet,
        CancellationToken cancellationToken,
        params string[] canonicalHeaders)
    {
        var headersCols = canonicalHeaders.ToDictionary<string, string, ExcelCellAddress?>(k => k, v => null);
        var usedCells = workSheet.Cells.Where(cell => !string.IsNullOrEmpty(cell.Text));
        var scannedCells = 0;
        foreach (var cell in usedCells)
        {
            ThrowIfCancellationRequested(cancellationToken, ref scannedCells);
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

    private Dictionary<int, SwimStyle> FindSwimStyles(ExcelWorksheet workSheet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Dictionary<int, SwimStyle> swimStyles = new();
        var startCell = FindFirstHeaderFromList(workSheet, StrokeHeaderNames);
        if (startCell is null) return swimStyles;
        var startCol = startCell.Column;
        var endCol = workSheet.Dimension.End.Column;
        var distanceRow = startCell.Row - 1;
        var strokeRow = startCell.Row;
        for (var col = startCol; col <= endCol; col++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var distanceText = workSheet.Cells[distanceRow, col].Text;
            if (string.IsNullOrWhiteSpace(distanceText)) continue;
            if (!int.TryParse(distanceText, out var distance))
            {
                _warnings.Add(string.Format(
                    CultureInfo.CurrentUICulture,
                    EntryImportStrings.DistanceParseFailed_Format,
                    MessageLocation(workSheet.Name, distanceRow, col)));
                continue;
            }

            var strokeText = workSheet.Cells[strokeRow, col].Text;
            if (string.IsNullOrWhiteSpace(strokeText)) continue;
            if (!EntryDocumentEnumParser.TryParseStroke(strokeText, out var style))
            {
                _warnings.Add(string.Format(
                    CultureInfo.CurrentUICulture,
                    EntryImportStrings.StrokeParseFailed_Format,
                    MessageLocation(workSheet.Name, strokeRow, col)));
                continue;
            }

            var swimStyle = dbAccess.GetOrAddIndividualSwimStyleByParameters(distance, style);
            log.Info(EntityLogFormatter.FormatOperation("Resolve", swimStyle));
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
                errors.Add(string.Format(
                    CultureInfo.CurrentUICulture,
                    EntryImportStrings.HeaderNotFound_Format,
                    header));
            }
        }

        return errors;
    }

    private List<Athlete> ReadAthletes(
        ExcelWorksheet workSheet,
        Dictionary<string, ExcelCellAddress?> athleteHeaders,
        Dictionary<int, SwimStyle> swimStyleHeaders,
        CancellationToken cancellationToken)
    {
        List<Athlete> athletes = [];
        var fromRowAthlete = athleteHeaders.First(pair => pair.Key == FIRSTNAME_HEADER).Value!.Row + 1;
        while (workSheet.Cells[fromRowAthlete, athleteHeaders[FIRSTNAME_HEADER]!.Column].IsEmpty()) fromRowAthlete++;
        var toRowAthlete = workSheet.Dimension.End.Row;
        for (var row = fromRowAthlete; row <= toRowAthlete; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hasErrors = false;
            var firstName = workSheet.Cells[row, athleteHeaders[FIRSTNAME_HEADER]!.Column].Text;
            if (string.IsNullOrWhiteSpace(firstName))
            {
                _warnings.Add(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        EntryImportStrings.AthleteFirstNameInvalid_Format,
                        MessageLocation(workSheet.Name, row, athleteHeaders[FIRSTNAME_HEADER]!.Column)));
                hasErrors = true;
            }

            var lastName = workSheet.Cells[row, athleteHeaders[LASTNAME_HEADER]!.Column].Text;
            if (string.IsNullOrWhiteSpace(lastName))
            {
                _warnings.Add(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        EntryImportStrings.AthleteLastNameInvalid_Format,
                        MessageLocation(workSheet.Name, row, athleteHeaders[LASTNAME_HEADER]!.Column)));
                hasErrors = true;
            }

            if (!int.TryParse(workSheet.Cells[row, athleteHeaders[BIRTH_YEAR_HEADER]!.Column].Text,
                    out var yearOfBirth))
            {
                _warnings.Add(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        EntryImportStrings.AthleteBirthYearInvalid_Format,
                        MessageLocation(workSheet.Name, row, athleteHeaders[BIRTH_YEAR_HEADER]!.Column)));
                hasErrors = true;
            }

            if (!EntryDocumentEnumParser.TryParseGender(workSheet.Cells[row, athleteHeaders[GENDER_HEADER]!.Column].Text,
                    out var gender))
            {
                _warnings.Add(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        EntryImportStrings.AthleteGenderInvalid_Format,
                        MessageLocation(workSheet.Name, row, athleteHeaders[GENDER_HEADER]!.Column)));
                hasErrors = true;
            }

            var category = ResolveCategory(workSheet, athleteHeaders, row);
            if (hasErrors) continue;
            var athlete = dbAccess.GetOrAddAthlete(firstName, lastName, yearOfBirth, gender, category);
            log.Info(EntityLogFormatter.FormatOperation("Resolve", athlete));
            var entryCols = swimStyleHeaders.Keys.Where(col => workSheet.Cells[row, col].Value != null &&
                                                               !string.IsNullOrWhiteSpace(
                                                                   workSheet.Cells[row, col].Text));
            var entries = new List<Entry>();
            foreach (var col in entryCols)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cellText = workSheet.Cells[row, col].Text.Trim();
                var cell = workSheet.Cells[row, col];
                var entry = dbAccess.GetOrAddEntry(athlete, swimStyleHeaders[col],
                    ResolveScoring(cell),
                    EntryTimeRegex().IsMatch(cellText)
                        ? ConvertEntryTimeToHundreds(cellText)
                        : null);
                entries.Add(entry);
                log.Info(EntityLogFormatter.FormatOperation("Resolve", entry));
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
        var clubName = workSheet.Cells[row, col].Text;
        if (string.Equals(clubName?.Trim(), HeaderAliases[CLUB_NAME_HEADER].First(),
                StringComparison.OrdinalIgnoreCase) ||
            HeaderAliases[CLUB_NAME_HEADER].Contains(clubName ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            while (workSheet.Cells[row, col + 1].IsEmpty()) row++;
            clubName = workSheet.Cells[row, col + 1].Text;
        }

        if (string.IsNullOrWhiteSpace(clubName))
        {
            _warnings.Add(string.Format(
                CultureInfo.CurrentUICulture,
                EntryImportStrings.ClubNameNotFound_PersonalScoring_Format,
                MessageLocation(workSheet.Name, null, null)));
            return null;
        }

        var club = dbAccess.GetOrAddClub(clubName);
        log.Info(EntityLogFormatter.FormatOperation("Resolve", club));
        return club;
    }

    private void LogWorksheetMessages(string worksheetName)
    {
        foreach (var warning in _warnings)
            log.Warning($"Entry import worksheet \"{worksheetName}\": {warning}");
        foreach (var error in _errors)
            log.Warning($"Entry import worksheet \"{worksheetName}\" error: {error}");
    }

    private static int ConvertEntryTimeToHundreds(string entryTime)
    {
        var matches = Regex.Matches(entryTime, @"\d+");
        if (matches.Count == 0)
            return 0;

        var hasFraction = Regex.IsMatch(entryTime, @"\D\d{1,2}\s*$");
        if (hasFraction)
        {
            var hundredths = int.Parse(matches[^1].Value);
            var seconds = int.Parse(matches[^2].Value);
            var minutes = matches.Count > 2 ? int.Parse(matches[0].Value) : 0;
            return minutes * 6000 + seconds * 100 + hundredths;
        }

        var secondsOnly = int.Parse(matches[^1].Value);
        var minutesOnly = matches.Count > 1 ? int.Parse(matches[0].Value) : 0;
        return minutesOnly * 6000 + secondsOnly * 100;
    }

    private bool ResolveScoring(ExcelRange cell)
    {
        var isHighlighted = cell.Style.Fill.PatternType != ExcelFillStyle.None;
        return _highlightScoringMode == EntryImportHighlightScoringMode.HighlightedInCompetition
            ? isHighlighted
            : !isHighlighted;
    }

    private string MessageLocation(string worksheetName, int? row, int? col)
    {
        return $"'{worksheetName}'[{row ?? '?'}:{col ?? '?'}]";
    }

    private static void ThrowIfCancellationRequested(CancellationToken cancellationToken, ref int counter,
        int interval = 32)
    {
        if (++counter % interval == 0)
            cancellationToken.ThrowIfCancellationRequested();
    }

    private Category ResolveCategory(
        ExcelWorksheet workSheet,
        Dictionary<string, ExcelCellAddress?> athleteHeaders,
        int row)
    {
        if (!athleteHeaders.TryGetValue(CATEGORY_HEADER, out var categoryHeader) || categoryHeader is null)
            return Category.NoCategory;

        var categoryText = workSheet.Cells[row, categoryHeader.Column].Text;
        if (string.IsNullOrWhiteSpace(categoryText))
            return Category.NoCategory;

        if (EntryDocumentEnumParser.TryParseCategory(categoryText, out var category))
            return category;

        _warnings.Add(
            string.Format(
                CultureInfo.CurrentUICulture,
                EntryImportStrings.AthleteCategoryInvalid_Format,
                MessageLocation(workSheet.Name, row, categoryHeader.Column)));
        return Category.NoCategory;
    }

    private static ExcelPackage OpenEntryFile(string filePath)
    {
        try
        {
            return ExcelPackageLoader.Open(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    EntryImportStrings.FileBusyOrUnavailable_Format,
                    Path.GetFileName(filePath)),
                ex);
        }
    }
}