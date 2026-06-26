using BizLogic.HeatAllocation;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.HeatService;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;

namespace Tests.Heats;

[TestFixture]
public sealed class EmptyHeatRemoverTest : DatabaseTestFixture
{
    private HeatService _heatService = null!;
    private TestDataSeeder _seeder = null!;

    [SetUp]
    public void SetUpService()
    {
        _heatService = new HeatService(Context, NullAppLog.Instance);
        _seeder = new TestDataSeeder(Context);
    }

    [Test]
    public async Task DeleteHeatPosition_RemovesEmptyHeat()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle);
        var (_, _, entry) = _seeder.SeedEntry(
            swimEvent,
            swimStyle,
            "Ivan",
            "Ivanov",
            2005,
            Gender.Male,
            1000);
        _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEvent.Id, HeatOrder.FromWeakToStrong, minHeatSize: 1));

        var heatId = await Context.HeatPositions
            .Where(position => position.EntryId == entry.Id)
            .Select(position => position.HeatId)
            .SingleAsync();
        Assert.That(await Context.Heats.CountAsync(heat => heat.SwimEventId == swimEvent.Id), Is.EqualTo(1));

        await _heatService.DeleteHeatPositionAsync(heatId, entry.Id);

        Context.ChangeTracker.Clear();
        Assert.That(await Context.Heats.CountAsync(heat => heat.SwimEventId == swimEvent.Id), Is.EqualTo(0));
        Assert.That(await Context.HeatPositions.AnyAsync(position => position.HeatId == heatId), Is.False);
    }
}
