using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryDocumentReaderService;
using Tests.TestInfrastructure;

namespace Tests.EntryDocumentReader;

[TestFixture]
public class EntryDocumentReaderTest : DatabaseTestFixture
{
    private IEntryDocumentReaderService _service = null!;
    private readonly string _oneTeamRussianXlsx = ResourcePath("Resources", "EntryDocument", "Тестовая команда.xlsx");
    private readonly string _oneTeamEnglishXlsx = ResourcePath("Resources", "EntryDocument", "Test club.xlsx");
    private string? _tempFilePath;

    [SetUp]
    public void SetUpService()
    {
        _service = new EntryDocumentReaderService(Context);
    }

    [TearDown]
    public void TearDownTempFiles()
    {
        if (_tempFilePath is not null && File.Exists(_tempFilePath))
            File.Delete(_tempFilePath);
        _tempFilePath = null;
    }

    [Test]
    public void ReadXlsxWithOneTeamRussian()
    {
        var result = _service.ReadWithStats(_oneTeamRussianXlsx);
        Assert.That(result.documents, Has.Count.EqualTo(1));
        AssertClubAthletes("Тестовая команда", isRussian: true);
    }

    [Test]
    public void ReadXlsxWithOneTeamEnglish()
    {
        var result = _service.ReadWithStats(_oneTeamEnglishXlsx);
        Assert.That(result.documents, Has.Count.EqualTo(1));
        AssertClubAthletes("Test club", isRussian: false);
    }

    [Test]
    public void ReadXlsWithOneTeamRussian()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xls");
        EntryDocumentFileHelper.ConvertXlsxToXls(_oneTeamRussianXlsx, _tempFilePath);

        var result = _service.ReadWithStats(_tempFilePath);
        Assert.That(result.documents, Has.Count.EqualTo(1));
        AssertClubAthletes("Тестовая команда", isRussian: true);
    }

    [Test]
    public void ReadXlsxWithMultipleTeams()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        EntryDocumentFileHelper.CreateTwoTeamWorkbook(_oneTeamRussianXlsx, _tempFilePath, "Вторая команда");

        var result = _service.ReadWithStats(_tempFilePath);
        Assert.That(result.documents, Has.Count.EqualTo(2));

        var clubs = Context.Clubs
            .Include(club => club.Athletes)
            .OrderBy(club => club.Name)
            .ToList();
        Assert.That(clubs, Has.Count.EqualTo(2));
        Assert.That(clubs[0].Name, Is.EqualTo("Вторая команда"));
        Assert.That(clubs[0].Athletes, Has.Count.EqualTo(3));
        Assert.That(clubs[1].Name, Is.EqualTo("Тестовая команда"));
        Assert.That(clubs[1].Athletes, Has.Count.EqualTo(9));
    }

    private void AssertClubAthletes(string expectedClubName, bool isRussian)
    {
        var clubs = Context.Clubs
            .Include(club => club.Athletes)
            .ThenInclude(athlete => athlete.Entries)
            .ThenInclude(entry => entry.SwimStyle)
            .ToList();
        Assert.That(clubs, Has.Count.EqualTo(1));
        var club = clubs[0];
        Assert.That(club.Name, Is.EqualTo(expectedClubName));
        Assert.That(club.Athletes, Has.Count.EqualTo(9));

        var athletes = club.Athletes.OrderBy(athlete => athlete.FirstName).ToList();
        for (var order = 0; order < athletes.Count; order++)
        {
            var athlete = athletes[order];
            using (Assert.EnterMultipleScope())
            {
                if (isRussian)
                {
                    Assert.That(athlete.FirstName, Is.EqualTo($"Имя{order}"));
                    Assert.That(athlete.LastName, Is.EqualTo($"Фамилия{order}"));
                }
                else
                {
                    Assert.That(athlete.FirstName, Is.EqualTo($"Firstname{order}"));
                    Assert.That(athlete.LastName, Is.EqualTo($"Lastname{order}"));
                }

                Assert.That(athlete.YearOfBirth, Is.EqualTo(2000 + order));
                Assert.That(athlete.Category, Is.EqualTo((Category)order));
                Assert.That(athlete.Gender, Is.EqualTo((Gender)(order % 2)));
                var entries = athlete.Entries.OrderBy(entry => entry.SwimStyle.Stroke).ToList();
                Assert.That(entries, Has.Count.EqualTo(2));
                Assert.That(entries[0].SwimStyle.Distance, Is.EqualTo(50));
                Assert.That(entries[0].SwimStyle.Stroke, Is.EqualTo(Stroke.Back));
                Assert.That(entries[0].EntryTime, Is.EqualTo(3000 + order * 100 + order));
                Assert.That(entries[0].Scoring, Is.True);
                Assert.That(entries[1].SwimStyle.Distance, Is.EqualTo(50));
                Assert.That(entries[1].SwimStyle.Stroke, Is.EqualTo(Stroke.Free));
                Assert.That(entries[1].EntryTime, Is.EqualTo(2000 + order * 100 + order));
                Assert.That(entries[1].Scoring, Is.True);
            }
        }
    }
}
