using BizLogic.HeatAllocation;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.HeatService;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;

namespace Tests.HeatAllocation;

[TestFixture]
public class HeatAllocationTest : DatabaseTestFixture
{
    private HeatService _heatService = null!;
    private TestDataSeeder _seeder = null!;

    [SetUp]
    public void SetUpServices()
    {
        _heatService = new HeatService(Context, NullAppLog.Instance);
        _seeder = new TestDataSeeder(Context);
    }

    [Test]
    public void AllocateEntriesToHeats_CreatesWeakAndStrongHeats()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle);

        for (var index = 0; index < 10; index++)
        {
            _seeder.SeedEntry(
                swimEvent,
                swimStyle,
                $"Athlete{index}",
                "Tester",
                2005,
                Gender.Male,
                1000 + index * 100);
        }

        var result = _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEvent.Id, HeatOrder.FromWeakToStrong, minHeatSize: 2));

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Heats, Has.Count.EqualTo(2));

        var heats = Context.Heats
            .AsNoTracking()
            .Include(heat => heat.Positions)
            .ThenInclude(position => position.Entry)
            .Where(heat => heat.SwimEventId == swimEvent.Id)
            .OrderBy(heat => heat.Number)
            .ToList();

        Assert.That(heats, Has.Count.EqualTo(2));
        var weakHeat = heats.Single(heat => heat.Positions.Count == 4);
        var strongHeat = heats.Single(heat => heat.Positions.Count == 6);
        Assert.That(weakHeat.Number, Is.LessThan(strongHeat.Number));

        var strongHeatTimes = strongHeat.Positions
            .Select(position => position.Entry!.EntryTime)
            .OrderBy(time => time)
            .ToList();
        Assert.That(strongHeatTimes, Is.EqualTo(new[] { 1000, 1100, 1200, 1300, 1400, 1500 }));

        var weakHeatTimes = weakHeat.Positions
            .Select(position => position.Entry!.EntryTime)
            .OrderBy(time => time)
            .ToList();
        Assert.That(weakHeatTimes, Is.EqualTo(new[] { 1600, 1700, 1800, 1900 }));
    }

    [Test]
    public void AllocateEntriesToHeats_CreatesOnlyFullHeats_WhenEntryCountIsDivisibleByLaneCount()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle);

        for (var index = 0; index < 12; index++)
        {
            _seeder.SeedEntry(
                swimEvent,
                swimStyle,
                $"Athlete{index}",
                "Tester",
                2005,
                Gender.Male,
                1000 + index * 100);
        }

        _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEvent.Id, HeatOrder.FromWeakToStrong, minHeatSize: 2));

        var heats = Context.Heats
            .AsNoTracking()
            .Include(heat => heat.Positions)
            .Where(heat => heat.SwimEventId == swimEvent.Id)
            .OrderBy(heat => heat.Number)
            .ToList();

        Assert.That(heats, Has.Count.EqualTo(2));
        Assert.That(heats.All(heat => heat.Positions.Count == 6), Is.True);
    }

    [Test]
    public void AllocateEntriesToHeats_AssignsLanesCenterOut()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle, laneMin: 1, laneMax: 6);

        for (var index = 0; index < 6; index++)
        {
            _seeder.SeedEntry(
                swimEvent,
                swimStyle,
                $"Athlete{index}",
                "Tester",
                2005,
                Gender.Male,
                1000 + index * 100);
        }

        _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEvent.Id, HeatOrder.FromWeakToStrong, minHeatSize: 2));

        var positions = Context.HeatPositions
            .AsNoTracking()
            .Include(position => position.Entry)
            .Where(position => position.Heat.SwimEventId == swimEvent.Id)
            .ToList();

        var fastestEntry = positions.Single(position => position.Entry!.EntryTime == 1000);
        Assert.That(fastestEntry.Lane, Is.EqualTo(3));

        var seedingOrder = positions
            .OrderBy(position => Array.IndexOf(new[] { 3, 4, 2, 5, 1, 6 }, position.Lane))
            .Select(position => position.Entry!.EntryTime)
            .ToList();
        Assert.That(seedingOrder, Is.EqualTo(new[] { 1000, 1100, 1200, 1300, 1400, 1500 }));
    }

    [Test]
    public void AllocateEntriesToHeats_ReversesHeatNumbers_WhenOrderIsFromStrongToWeak()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        var swimEvent = _seeder.SeedSwimEvent(ageGroup, swimStyle);

        for (var index = 0; index < 10; index++)
        {
            _seeder.SeedEntry(
                swimEvent,
                swimStyle,
                $"Athlete{index}",
                "Tester",
                2005,
                Gender.Male,
                1000 + index * 100);
        }

        _heatService.AllocateEntriesToHeats(
            new HeatAllocationParameters(swimEvent.Id, HeatOrder.FromStrongToWeak, minHeatSize: 2));

        var heatNumbers = Context.Heats
            .AsNoTracking()
            .Where(heat => heat.SwimEventId == swimEvent.Id)
            .OrderBy(heat => heat.Number)
            .Select(heat => heat.Number)
            .ToList();

        Assert.That(heatNumbers, Is.EqualTo(new[] { 1, 2 }));
        var heatsByNumber = Context.Heats
            .AsNoTracking()
            .Include(heat => heat.Positions)
            .Where(heat => heat.SwimEventId == swimEvent.Id)
            .ToDictionary(heat => heat.Number);
        Assert.That(heatsByNumber[1].Positions, Has.Count.EqualTo(6));
        Assert.That(heatsByNumber[2].Positions, Has.Count.EqualTo(4));
    }
}
