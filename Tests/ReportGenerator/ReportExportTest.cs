using BizLogic.Resources;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ServiceLayer.EntryService;
using ServiceLayer.HeatService;
using ServiceLayer.Logging;
using ServiceLayer.ReportGeneratorService;
using Tests.TestInfrastructure;
using BizLogic.HeatAllocation;

namespace Tests.ReportGenerator;

[TestFixture]
public class ReportExportTest : DatabaseTestFixture
{
    private ReportExportService _reportService = null!;
    private HeatService _heatService = null!;
    private TestDataSeeder _seeder = null!;
    private readonly List<string> _tempFiles = [];

    [SetUp]
    public void SetUpServices()
    {
        _reportService = new ReportExportService(Context, new EntryService(Context, NullAppLog.Instance), NullAppLog.Instance);
        _heatService = new HeatService(Context, NullAppLog.Instance);
        _seeder = new TestDataSeeder(Context);
    }

    [TearDown]
    public void TearDownTempFiles()
    {
        foreach (var tempFile in _tempFiles.Where(File.Exists))
            File.Delete(tempFile);
        _tempFiles.Clear();
    }

    [Test]
    public void ExportEntryListReport_CreatesWorksheetWithEntries()
    {
        var swimEventId = SeedEventWithEntries().swimEventId;
        var outputPath = CreateTempOutputPath();

        _reportService.ExportToExcel(new ReportExportOptions
        {
            SwimEventIds = [swimEventId],
            OutputFilePath = outputPath,
            IncludeEntryList = true
        });

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[ReportExcelStrings.Sheet_EntryList];
        Assert.That(worksheet, Is.Not.Null);
        Assert.That(worksheet.Cells[1, 1].Text, Does.Contain("50"));
        Assert.That(FindCellText(worksheet, "Ivan Ivanov"), Is.Not.Null);
        Assert.That(FindCellText(worksheet, "Petr Petrov"), Is.Not.Null);
    }

    [Test]
    public void ExportStartListReport_CreatesWorksheetWithHeats()
    {
        var swimEventId = SeedEventWithEntries().swimEventId;
        _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEventId, HeatOrder.FromWeakToStrong, minHeatSize: 2));
        var outputPath = CreateTempOutputPath();

        _reportService.ExportToExcel(new ReportExportOptions
        {
            SwimEventIds = [swimEventId],
            OutputFilePath = outputPath,
            IncludeStartList = true
        });

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[ReportExcelStrings.Sheet_StartList];
        Assert.That(worksheet, Is.Not.Null);
        Assert.That(FindCellText(worksheet, "Ivan Ivanov"), Is.Not.Null);
        Assert.That(FindCellText(worksheet, "Petr Petrov"), Is.Not.Null);
        Assert.That(worksheet.Cells[1, 1].Text, Does.Contain("50"));
    }

    [Test]
    public void ExportFinishListReport_CreatesWorksheetWithResults()
    {
        var swimEventId = SeedEventWithFinishedEntries().swimEventId;
        var outputPath = CreateTempOutputPath();

        _reportService.ExportToExcel(new ReportExportOptions
        {
            SwimEventIds = [swimEventId],
            OutputFilePath = outputPath,
            IncludeFinishList = true
        });

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[ReportExcelStrings.Sheet_FinishList];
        Assert.That(worksheet, Is.Not.Null);
        Assert.That(FindCellText(worksheet, "Ivan Ivanov"), Is.Not.Null);
        Assert.That(FindCellText(worksheet, "Petr Petrov"), Is.Not.Null);
        Assert.That(FindCellText(worksheet, "8"), Is.Not.Null);
        Assert.That(FindCellText(worksheet, "7"), Is.Not.Null);
    }

    [Test]
    public void ExportCombinedResultsReport_CreatesWorksheetWithTotals()
    {
        var ageGroupId = SeedCombinedResultsScenario();
        var outputPath = CreateTempOutputPath();

        _reportService.ExportCombinedResultsToExcel(new CombinedResultsExportOptions
        {
            AgeGroupIds = [ageGroupId],
            OutputFilePath = outputPath
        });

        using var package = new ExcelPackage(new FileInfo(outputPath));
        var worksheet = package.Workbook.Worksheets[ReportExcelStrings.Sheet_CombinedResults];
        Assert.That(worksheet, Is.Not.Null);
        Assert.That(FindCellText(worksheet, "Ivan Ivanov"), Is.Not.Null);
        Assert.That(FindCellText(worksheet, "Petr Petrov"), Is.Not.Null);
        Assert.That(ContainsCellText(worksheet, "15"), Is.True);
        Assert.That(ContainsCellText(worksheet, "12"), Is.True);
    }

    [Test]
    public void ExportAllEventReports_CreatesAllWorksheetsInSingleFile()
    {
        var swimEventId = SeedEventWithFinishedEntries().swimEventId;
        _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEventId, HeatOrder.FromWeakToStrong, minHeatSize: 2));
        var outputPath = CreateTempOutputPath();

        _reportService.ExportToExcel(new ReportExportOptions
        {
            SwimEventIds = [swimEventId],
            OutputFilePath = outputPath,
            IncludeEntryList = true,
            IncludeStartList = true,
            IncludeFinishList = true
        });

        using var package = new ExcelPackage(new FileInfo(outputPath));
        Assert.That(package.Workbook.Worksheets[ReportExcelStrings.Sheet_EntryList], Is.Not.Null);
        Assert.That(package.Workbook.Worksheets[ReportExcelStrings.Sheet_StartList], Is.Not.Null);
        Assert.That(package.Workbook.Worksheets[ReportExcelStrings.Sheet_FinishList], Is.Not.Null);
    }

    private (int swimEventId, Athlete first, Athlete second) SeedEventWithEntries()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle);
        var first = _seeder.SeedEntry(swimEvent, swimStyle, "Ivan", "Ivanov", 2005, Gender.Male, 1250);
        var second = _seeder.SeedEntry(swimEvent, swimStyle, "Petr", "Petrov", 2005, Gender.Male, 1320);
        return (swimEvent.Id, first.Athlete, second.Athlete);
    }

    private (int swimEventId, Athlete first, Athlete second) SeedEventWithFinishedEntries()
    {
        var seeded = SeedEventWithEntries();
        var entries = Context.Entries
            .Where(entry => entry.SwimEventId == seeded.swimEventId)
            .OrderBy(entry => entry.EntryTime)
            .ToList();
        entries[0].FinishTime = 1240;
        entries[0].Points = 8;
        entries[0].Status = EntryStatus.FINISH;
        entries[1].FinishTime = 1310;
        entries[1].Points = 7;
        entries[1].Status = EntryStatus.FINISH;
        Assert.That(Context.SaveChangesWithValidation(), Is.Empty);
        return seeded;
    }

    private int SeedCombinedResultsScenario()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var freeStyle = _seeder.SeedSwimStyle(50, Stroke.Free);
        var backStyle = _seeder.SeedSwimStyle(50, Stroke.Back);
        var freeEvent = _seeder.SeedSwimEvent(ageGroup, freeStyle, order: 1);
        var backEvent = _seeder.SeedSwimEvent(ageGroup, backStyle, order: 2);

        var ivanFree = _seeder.SeedEntry(freeEvent, freeStyle, "Ivan", "Ivanov", 2005, Gender.Male, 1250,
            finishTime: 1240, points: 8, status: EntryStatus.FINISH);
        _seeder.SeedEntry(backEvent, backStyle, "Ivan", "Ivanov", 2005, Gender.Male, 1320, finishTime: 1310,
            points: 7, status: EntryStatus.FINISH, existingAthlete: ivanFree.Athlete);

        var petrFree = _seeder.SeedEntry(freeEvent, freeStyle, "Petr", "Petrov", 2005, Gender.Male, 1300,
            finishTime: 1290, points: 7, status: EntryStatus.FINISH);
        _seeder.SeedEntry(backEvent, backStyle, "Petr", "Petrov", 2005, Gender.Male, 1350, finishTime: 1340,
            points: 5, status: EntryStatus.FINISH, existingAthlete: petrFree.Athlete);

        return ageGroup.Id;
    }

    private string CreateTempOutputPath()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        _tempFiles.Add(outputPath);
        return outputPath;
    }

    private static string? FindCellText(ExcelWorksheet worksheet, string expectedText) =>
        ContainsCellText(worksheet, expectedText) ? expectedText : null;

    private static bool ContainsCellText(ExcelWorksheet worksheet, string expectedText)
    {
        if (worksheet.Dimension is null)
            return false;

        for (var row = 1; row <= worksheet.Dimension.End.Row; row++)
        {
            for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                if (worksheet.Cells[row, col].Text.Contains(expectedText, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }
}
