using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryService;
using ServiceLayer.HeatService;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;
using BizLogic.HeatAllocation;

namespace Tests.Event;

[TestFixture]
public sealed class SwimEventStatusTest : DatabaseTestFixture
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
    public async Task DeleteLastEntry_WithoutHeats_SetsEventStatusToEmpty()
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

        Context.ChangeTracker.Clear();
        Assert.That(await GetEventStatusAsync(swimEvent.Id), Is.EqualTo(SwimEventStatus.ENTRY));

        await _entryService.DeleteAsync(entry.Id);

        Context.ChangeTracker.Clear();
        Assert.That(await GetEventStatusAsync(swimEvent.Id), Is.EqualTo(SwimEventStatus.EMPTY));
    }

    [Test]
    public async Task DeleteLastEntry_WithHeats_SetsEventStatusToNotStarted()
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
        _seeder.SeedEntry(
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
        Assert.That(await GetEventStatusAsync(swimEvent.Id), Is.EqualTo(SwimEventStatus.NOT_STARTED));

        await _entryService.DeleteAsync(entry1.Id);

        Context.ChangeTracker.Clear();
        Assert.That(await GetEventStatusAsync(swimEvent.Id), Is.EqualTo(SwimEventStatus.NOT_STARTED));
    }

    [Test]
    public async Task DeleteAllEntries_WithHeats_RemovesHeats_AndSetsStatusEmpty()
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
        Assert.That(await Context.Heats.CountAsync(heat => heat.SwimEventId == swimEvent.Id), Is.GreaterThan(0));

        await _entryService.DeleteAsync(entry1.Id);
        await _entryService.DeleteAsync(entry2.Id);

        Context.ChangeTracker.Clear();
        Assert.That(await Context.Heats.CountAsync(heat => heat.SwimEventId == swimEvent.Id), Is.EqualTo(0));
        Assert.That(await GetEventStatusAsync(swimEvent.Id), Is.EqualTo(SwimEventStatus.EMPTY));
    }

    private Task<SwimEventStatus> GetEventStatusAsync(int swimEventId) =>
        Context.SwimEvents
            .AsNoTracking()
            .Where(swimEvent => swimEvent.Id == swimEventId)
            .Select(swimEvent => swimEvent.Status)
            .SingleAsync();
}
