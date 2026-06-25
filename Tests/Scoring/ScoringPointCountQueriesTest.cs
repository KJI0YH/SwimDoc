using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryService;
using Tests.TestInfrastructure;

namespace Tests.Scoring;

[TestFixture]
public class ScoringPointCountQueriesTest : DatabaseTestFixture
{
    [Test]
    public async Task GetClubPointCountsAsync_LoadsForSeededClub()
    {
        var seeder = new TestDataSeeder(Context);
        var ageGroup = seeder.SeedAgeGroup();
        var swimStyle = seeder.SeedSwimStyle();
        var swimEvent = seeder.SeedSwimEvent(ageGroup, swimStyle);
        var (_, _, _) = seeder.SeedEntry(swimEvent, swimStyle, "Ivan", "Ivanov", 2005, Gender.Male, 3000, 2800, 10);

        var clubId = await Context.Clubs.Select(c => c.Id).SingleAsync();
        var pointCounts = await ScoringPointCountQueries.GetClubPointCountsAsync(Context, [clubId]);
        Assert.That(pointCounts[clubId], Is.EqualTo(10));
    }

    [Test]
    public async Task GetClubPointCountsAsync_ReturnsEmptyForNoClubs()
    {
        var pointCounts = await ScoringPointCountQueries.GetClubPointCountsAsync(Context, []);
        Assert.That(pointCounts, Is.Empty);
    }
}
