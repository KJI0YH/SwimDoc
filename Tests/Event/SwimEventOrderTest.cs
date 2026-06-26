using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.BaseTimeRepository;
using ServiceLayer.EventService;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;

namespace Tests.Event;

[TestFixture]
public sealed class SwimEventOrderTest : DatabaseTestFixture
{
    private EventService _eventService = null!;
    private TestDataSeeder _seeder = null!;

    [SetUp]
    public void SetUpService()
    {
        _eventService = new EventService(Context, new StubBaseTimeRepository(), NullAppLog.Instance);
        _seeder = new TestDataSeeder(Context);
    }

    [Test]
    public async Task CreateAsync_InsertsAtOrder_ShiftsExistingEventsUp()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var firstStyle = _seeder.SeedSwimStyle(50, Stroke.Free);
        var secondStyle = _seeder.SeedSwimStyle(100, Stroke.Free);
        var thirdStyle = _seeder.SeedSwimStyle(200, Stroke.Free);
        _seeder.SeedSwimEvent(ageGroup, firstStyle, order: 1);
        _seeder.SeedSwimEvent(ageGroup, secondStyle, order: 2);
        _seeder.SeedSwimEvent(ageGroup, thirdStyle, order: 3);

        var insertedStyle = _seeder.SeedSwimStyle(400, Stroke.Free);
        var newEvent = BuildSwimEvent(ageGroup, insertedStyle, order: 2);
        var (_, errors) = await _eventService.CreateAsync(newEvent);
        Assert.That(errors, Is.Empty);

        var orders = await GetOrdersAsync();
        Assert.That(orders, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public async Task UpdateAsync_MovesEventEarlier_ShiftsIntermediateEventsDown()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var styles = new[]
        {
            _seeder.SeedSwimStyle(50, Stroke.Free),
            _seeder.SeedSwimStyle(100, Stroke.Free),
            _seeder.SeedSwimStyle(200, Stroke.Free),
            _seeder.SeedSwimStyle(400, Stroke.Free)
        };
        _seeder.SeedSwimEvent(ageGroup, styles[0], order: 1);
        _seeder.SeedSwimEvent(ageGroup, styles[1], order: 2);
        _seeder.SeedSwimEvent(ageGroup, styles[2], order: 3);
        var seeded = _seeder.SeedSwimEvent(ageGroup, styles[3], order: 4);
        var moved = await Context.SwimEvents.AsNoTracking().SingleAsync(swimEvent => swimEvent.Id == seeded.Id);
        moved.Order = 2;
        var (_, errors) = await _eventService.UpdateAsync(moved);
        Assert.That(errors, Is.Empty);

        var orders = await GetOrdersAsync();
        Assert.That(orders, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        Assert.That(await GetOrderAsync(moved.Id), Is.EqualTo(2));
    }

    [Test]
    public async Task CreateAsync_InsertsAtFreeOrder_LeavesExistingOrdersUnchanged()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var styles = new[]
        {
            _seeder.SeedSwimStyle(50, Stroke.Free),
            _seeder.SeedSwimStyle(100, Stroke.Free),
            _seeder.SeedSwimStyle(200, Stroke.Free)
        };
        _seeder.SeedSwimEvent(ageGroup, styles[0], order: 1);
        _seeder.SeedSwimEvent(ageGroup, styles[1], order: 2);
        _seeder.SeedSwimEvent(ageGroup, styles[2], order: 3);

        var insertedStyle = _seeder.SeedSwimStyle(400, Stroke.Free);
        var newEvent = BuildSwimEvent(ageGroup, insertedStyle, order: 5);
        var (_, errors) = await _eventService.CreateAsync(newEvent);
        Assert.That(errors, Is.Empty);

        var orders = await GetOrdersAsync();
        Assert.That(orders, Is.EqualTo(new[] { 1, 2, 3, 5 }));
    }

    [Test]
    public async Task UpdateAsync_MovesEventToFreeOrder_LeavesOtherOrdersUnchanged()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var styles = new[]
        {
            _seeder.SeedSwimStyle(50, Stroke.Free),
            _seeder.SeedSwimStyle(100, Stroke.Free),
            _seeder.SeedSwimStyle(200, Stroke.Free),
            _seeder.SeedSwimStyle(400, Stroke.Free)
        };
        _seeder.SeedSwimEvent(ageGroup, styles[0], order: 1);
        _seeder.SeedSwimEvent(ageGroup, styles[1], order: 3);
        _seeder.SeedSwimEvent(ageGroup, styles[2], order: 5);
        var seeded = _seeder.SeedSwimEvent(ageGroup, styles[3], order: 10);
        var moved = await Context.SwimEvents.AsNoTracking().SingleAsync(swimEvent => swimEvent.Id == seeded.Id);
        moved.Order = 7;
        var (_, errors) = await _eventService.UpdateAsync(moved);
        Assert.That(errors, Is.Empty);

        var orders = await GetOrdersAsync();
        Assert.That(orders, Is.EqualTo(new[] { 1, 3, 5, 7 }));
        Assert.That(await GetOrderAsync(moved.Id), Is.EqualTo(7));
    }

    [Test]
    public async Task DeleteAsync_RemovesEventAndLeavesOrderGap()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var styles = new[]
        {
            _seeder.SeedSwimStyle(50, Stroke.Free),
            _seeder.SeedSwimStyle(100, Stroke.Free),
            _seeder.SeedSwimStyle(200, Stroke.Free)
        };
        _seeder.SeedSwimEvent(ageGroup, styles[0], order: 1);
        var deleted = _seeder.SeedSwimEvent(ageGroup, styles[1], order: 2);
        _seeder.SeedSwimEvent(ageGroup, styles[2], order: 3);

        await _eventService.DeleteAsync(deleted.Id);

        var orders = await GetOrdersAsync();
        Assert.That(orders, Is.EqualTo(new[] { 1, 3 }));
    }

    private async Task<int[]> GetOrdersAsync() =>
        await Context.SwimEvents
            .OrderBy(swimEvent => swimEvent.Order)
            .Select(swimEvent => swimEvent.Order)
            .ToArrayAsync();

    private Task<int> GetOrderAsync(int swimEventId) =>
        Context.SwimEvents
            .Where(swimEvent => swimEvent.Id == swimEventId)
            .Select(swimEvent => swimEvent.Order)
            .SingleAsync();

    private static SwimEvent BuildSwimEvent(AgeGroup ageGroup, SwimStyle swimStyle, int order) =>
        new()
        {
            Order = order,
            Date = DateOnly.FromDateTime(DateTime.Today),
            LaneMin = 1,
            LaneMax = 6,
            RoundParticipantsCount = 8,
            AgeGroupId = ageGroup.Id,
            SwimStyleId = swimStyle.Id
        };

    private sealed class StubBaseTimeRepository : IBaseTimeRepository
    {
        public int GetBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex) => 0;
        public void SetBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex, int baseTimeHundredths)
        {
        }
        public void Save()
        {
        }
    }
}
