using BizLogic.HeatAllocation;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryService;
using ServiceLayer.HeatService;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;

namespace Tests.Event;

[TestFixture]
public sealed class EmptyHeatAfterEntryDeleteTest : DatabaseTestFixture
{
    private EntryService _entryService = null!;
    private HeatService _heatService = null!;
    private TestDataSeeder _seeder = null!;

    [SetUp]
    public void SetUpService()
    {
        _entryService = new EntryService(Context, NullAppLog.Instance);
        _heatService = new HeatService(Context, NullAppLog.Instance);
        _seeder = new TestDataSeeder(Context);
    }

    [Test]
    public async Task DeleteAllEntries_RemovesAllEmptyHeats()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle);
        var (_, _, entry1) = _seeder.SeedEntry(
            swimEvent,
            swimStyle,
            "Ivan",
            "Ivanov",
            2005,
            Gender.Male,
            1000);
        var (_, _, entry2) = _seeder.SeedEntry(
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
        await _entryService.DeleteAsync(entry1.Id);
        await _entryService.DeleteAsync(entry2.Id);

        Context.ChangeTracker.Clear();
        Assert.That(await Context.Heats.CountAsync(heat => heat.SwimEventId == swimEvent.Id), Is.EqualTo(0));
        Assert.That(
            await Context.SwimEvents.AsNoTracking()
                .Where(se => se.Id == swimEvent.Id)
                .Select(se => se.Status)
                .SingleAsync(),
            Is.EqualTo(SwimEventStatus.EMPTY));
    }

    [Test]
    public async Task DeleteAllEntries_RemovesPreExistingEmptyHeats_AndSetsStatusEmpty()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle);
        var (_, _, entry1) = _seeder.SeedEntry(
            swimEvent,
            swimStyle,
            "Ivan",
            "Ivanov",
            2005,
            Gender.Male,
            1000);
        var (_, _, entry2) = _seeder.SeedEntry(
            swimEvent,
            swimStyle,
            "Petr",
            "Petrov",
            2005,
            Gender.Male,
            1100);
        _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEvent.Id, HeatOrder.FromWeakToStrong, minHeatSize: 2));
        await _heatService.SaveHeatWithPositionsAsync(
            new Heat
            {
                SwimEventId = swimEvent.Id,
                Number = 99,
                Status = HeatStatus.NOT_STARTED,
                Positions = []
            },
            isAdd: true);

        Context.ChangeTracker.Clear();
        Assert.That(await Context.Heats.CountAsync(heat => heat.SwimEventId == swimEvent.Id), Is.EqualTo(2));

        await _entryService.DeleteAsync(entry1.Id);
        await _entryService.DeleteAsync(entry2.Id);

        Context.ChangeTracker.Clear();
        Assert.That(await Context.Heats.CountAsync(heat => heat.SwimEventId == swimEvent.Id), Is.EqualTo(0));
        Assert.That(
            await Context.SwimEvents.AsNoTracking()
                .Where(se => se.Id == swimEvent.Id)
                .Select(se => se.Status)
                .SingleAsync(),
            Is.EqualTo(SwimEventStatus.EMPTY));
    }

    [Test]
    public async Task DeleteEntry_RemovesHeatWhenOrphanPositionsRemain()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle);
        var (_, _, entry1) = _seeder.SeedEntry(
            swimEvent,
            swimStyle,
            "Ivan",
            "Ivanov",
            2005,
            Gender.Male,
            1000);
        var (_, _, entry2) = _seeder.SeedEntry(
            swimEvent,
            swimStyle,
            "Petr",
            "Petrov",
            2005,
            Gender.Male,
            1100);
        _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEvent.Id, HeatOrder.FromWeakToStrong, minHeatSize: 2));
        var heatId = await Context.HeatPositions.Select(position => position.HeatId).FirstAsync();

        await Context.Database.ExecuteSqlRawAsync("DELETE FROM Entries WHERE Id = {0}", entry1.Id);
        Context.ChangeTracker.Clear();

        await _entryService.DeleteAsync(entry2.Id);

        Context.ChangeTracker.Clear();
        Assert.That(await Context.Heats.CountAsync(heat => heat.SwimEventId == swimEvent.Id), Is.EqualTo(0));
        Assert.That(await Context.HeatPositions.CountAsync(position => position.HeatId == heatId), Is.EqualTo(0));
    }
}
