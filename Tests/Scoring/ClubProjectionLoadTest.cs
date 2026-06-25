using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.ClubService;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;

namespace Tests.Scoring;

[TestFixture]
public class ClubProjectionLoadTest : DatabaseTestFixture
{
    [Test]
    public async Task SelectClub_LoadsRows()
    {
        var seeder = new TestDataSeeder(Context);
        var ageGroup = seeder.SeedAgeGroup();
        var swimStyle = seeder.SeedSwimStyle();
        var swimEvent = seeder.SeedSwimEvent(ageGroup, swimStyle);
        seeder.SeedEntry(swimEvent, swimStyle, "Ivan", "Ivanov", 2005, Gender.Male, 3000, 2800, 10);

        var clubService = new ClubService(Context, new NullAppLog());
        var projections = await clubService.Query()
            .Select(c => new
            {
                c.Id,
                c.Name,
                AthleteCount = c.Athletes.Count,
                EntryScoringCount = c.Athletes.Sum(a => a.Entries.Count(e => e.Scoring)),
                EntryPersonalCount = c.Athletes.Sum(a => a.Entries.Count(e => !e.Scoring)),
                RelayScoringCount = c.Relays.Count(r => r.Entry != null && r.Entry.Scoring),
                RelayPersonalCount = c.Relays.Count(r => r.Entry != null && !r.Entry.Scoring)
            })
            .ToListAsync();

        Assert.That(projections, Has.Count.EqualTo(1));
        Assert.That(projections[0].AthleteCount, Is.EqualTo(1));
    }
}
