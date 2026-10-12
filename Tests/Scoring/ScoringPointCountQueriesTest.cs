using DataLayer.EfClasses;
using DataLayer.EfCore;
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
    public async Task GetClubPointCountsAsync_IncludesRelayPoints()
    {
        var seeder = new TestDataSeeder(Context);
        var ageGroup = seeder.SeedAgeGroup();
        var swimStyle = seeder.SeedSwimStyle();
        var swimEvent = seeder.SeedSwimEvent(ageGroup, swimStyle);
        var (club, athlete, _) = seeder.SeedEntry(swimEvent, swimStyle, "Ivan", "Ivanov", 2005, Gender.Male, 3000, 2800, 10);

        var relayStyle = new SwimStyle { Distance = 50, Stroke = Stroke.Free, RelayCount = 4 };
        Context.SwimStyles.Add(relayStyle);
        Assert.That(Context.SaveChangesWithValidation(), Is.Empty);
        var relayEvent = seeder.SeedSwimEvent(ageGroup, relayStyle, order: 2);
        var relayEntry = new Entry
        {
            SwimStyleId = relayStyle.Id,
            SwimEventId = relayEvent.Id,
            SwimEvent = relayEvent,
            Points = 16,
            Status = EntryStatus.EVENT,
            Scoring = true,
            Relay = new Relay
            {
                Club = club,
                Positions = [new RelayPosition { Order = 1, Athlete = athlete }]
            }
        };
        Context.Entries.Add(relayEntry);
        Context.NormalizeEntry(relayEntry);
        Assert.That(Context.SaveChangesWithValidation(), Is.Empty);

        var pointCounts = await ScoringPointCountQueries.GetClubPointCountsAsync(Context, [club.Id]);
        Assert.That(pointCounts[club.Id], Is.EqualTo(26));
    }

    [Test]
    public async Task GetClubPointCountsAsync_ReturnsEmptyForNoClubs()
    {
        var pointCounts = await ScoringPointCountQueries.GetClubPointCountsAsync(Context, []);
        Assert.That(pointCounts, Is.Empty);
    }
}
