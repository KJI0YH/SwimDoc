using DataLayer.Display;
using DataLayer.EfClasses;
using DataLayer.QueryObjects;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.AgeGroupService;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;

namespace Tests.Scoring;

[TestFixture]
public class AgeGroupProjectionLoadTest : DatabaseTestFixture
{
    [Test]
    public async Task SelectAgeGroup_LoadsRowsWithParticipantCounts()
    {
        var seeder = new TestDataSeeder(Context);
        seeder.SeedAgeGroup();
        var ageGroup = seeder.SeedAgeGroup(name: "Second group", birthYearMin: 2011, birthYearMax: 2015);
        var swimStyle = seeder.SeedSwimStyle();
        var swimEvent = seeder.SeedSwimEvent(ageGroup, swimStyle);
        seeder.SeedEntry(swimEvent, swimStyle, "Ivan", "Ivanov", 2012, Gender.Male, 3000);

        var ageGroupService = new AgeGroupService(Context, new NullAppLog());
        var projections = await ageGroupService.Query().Page(0, 30)
            .Select(ag => new
            {
                ag.Id,
                ag.BirthYearMin,
                ag.BirthYearMax,
                ag.Gender
            })
            .ToListAsync();

        var athletes = await Context.Athletes.AsNoTracking()
            .Select(a => new { a.YearOfBirth, a.Gender })
            .ToListAsync();

        var counts = projections.Select(projection =>
            AgeGroupParticipantCounter.Count(
                projection.BirthYearMin,
                projection.BirthYearMax,
                projection.Gender,
                athletes.Select(a => (a.YearOfBirth, a.Gender)))).ToList();

        Assert.That(projections, Has.Count.EqualTo(2));
        Assert.That(counts.Sum(), Is.EqualTo(1));
    }
}