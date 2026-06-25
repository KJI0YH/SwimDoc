using BizLogic.EntryDocumentReader.Concrete;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ServiceLayer.AppSettings;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryImportSettings;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;

namespace Tests.EntryDocumentReader;

[TestFixture]
public class SwimStyleHeaderImportTest : DatabaseTestFixture
{
    [Test]
    public void ReadWithStats_ParsesStrokeAboveDistance_WithMergedStrokeCells()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        try
        {
            BuildWorkbook(path);
            var service = new EntryDocumentReaderService(
                Context,
                new EntryImportSettingsService(new AppSettingsStore()),
                NullAppLog.Instance);
            var result = service.ReadWithStats(path);
            Assert.That(result.documents, Has.Count.EqualTo(1));
            var club = Context.Clubs.Include(c => c.Athletes).ThenInclude(a => a.Entries).ThenInclude(e => e.SwimStyle)
                .Single();
            Assert.That(club.Athletes, Has.Count.EqualTo(1));
            var entries = club.Athletes.Single().Entries.OrderBy(e => e.SwimStyle.Distance).ToList();
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries[0].SwimStyle.Distance, Is.EqualTo(50));
            Assert.That(entries[0].SwimStyle.Stroke, Is.EqualTo(Stroke.Free));
            Assert.That(entries[1].SwimStyle.Distance, Is.EqualTo(100));
            Assert.That(entries[1].SwimStyle.Stroke, Is.EqualTo(Stroke.Free));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static void BuildWorkbook(string path)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Team");
        ws.Cells["A1"].Value = "Команда";
        ws.Cells["B1"].Value = "Dolphins";
        ws.Cells["A2"].Value = "Фамилия Имя";
        ws.Cells["B2"].Value = "Год рождения";
        ws.Cells["C2"].Value = "Пол";
        ws.Cells["D2"].Value = "Разряд";
        ws.Cells["E2"].Value = "Вольный стиль";
        ws.Cells["E3"].Value = 50;
        ws.Cells["F3"].Value = 100;
        ws.Cells["A4"].Value = "Иванов Иван";
        ws.Cells["B4"].Value = 2000;
        ws.Cells["C4"].Value = "Женщины";
        ws.Cells["D4"].Value = "МС";
        ws.Cells["E4"].Value = "25.00";
        ws.Cells["F4"].Value = "55.00";
        ws.Cells["E2:F2"].Merge = true;
        package.SaveAs(new FileInfo(path));
    }
}
