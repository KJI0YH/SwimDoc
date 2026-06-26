using BizLogic.HeatAllocation;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.AthleteService;
using ServiceLayer.HeatService;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;

namespace Tests.Event;

[TestFixture]
public sealed class EmptyHeatAfterAthleteDeleteTest : DatabaseTestFixture
{
    private AthleteService _athleteService = null!;
    private HeatService _heatService = null!;
    private TestDataSeeder _seeder = null!;

    [SetUp]
    public void SetUpService()
    {
        _athleteService = new AthleteService(Context, NullAppLog.Instance);
        _heatService = new HeatService(Context, NullAppLog.Instance);
        _seeder = new TestDataSeeder(Context);
    }

    [Test]
    public async Task DeleteAllAthletes_RemovesEmptyHeats_AndSetsStatusEmpty()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle);
        var (_, athlete1, _) = _seeder.SeedEntry(
            swimEvent,
            swimStyle,
            "Ivan",
            "Ivanov",
            2005,
            Gender.Male,
            1000);
        var (_, athlete2, _) = _seeder.SeedEntry(
            swimEvent,
            swimStyle,
            "Petr",
            "Petrov",
            2005,
            Gender.Male,
            1100);
        _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEvent.Id, HeatOrder.FromWeakToStrong, minHeatSize: 2));

        Context.ChangeTracker.Clear();
        await _athleteService.DeleteAsync(athlete1.Id);
        await _athleteService.DeleteAsync(athlete2.Id);

        Context.ChangeTracker.Clear();
        Assert.That(await Context.Heats.CountAsync(heat => heat.SwimEventId == swimEvent.Id), Is.EqualTo(0));
        Assert.That(
            await Context.SwimEvents.AsNoTracking()
                .Where(se => se.Id == swimEvent.Id)
                .Select(se => se.Status)
                .SingleAsync(),
            Is.EqualTo(SwimEventStatus.EMPTY));
    }
}
