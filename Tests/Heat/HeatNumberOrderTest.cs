using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.BaseTimeRepository;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.Logging;
using Tests.TestInfrastructure;

namespace Tests.Heats;

[TestFixture]
public sealed class HeatNumberOrderTest : DatabaseTestFixture
{
    private HeatService _heatService = null!;
    private EventService _eventService = null!;
    private TestDataSeeder _seeder = null!;

    [SetUp]
    public void SetUpServices()
    {
        _heatService = new HeatService(Context, NullAppLog.Instance);
        _eventService = new EventService(Context, new StubBaseTimeRepository(), NullAppLog.Instance);
        _seeder = new TestDataSeeder(Context);
    }

    [Test]
    public async Task InsertAtNumber_ShiftsExistingHeatsWithinEvent()
    {
        var swimEvent = SeedEvent();
        await SeedHeatAsync(swimEvent.Id, 1);
        await SeedHeatAsync(swimEvent.Id, 2);
        await SeedHeatAsync(swimEvent.Id, 3);

        Context.ChangeTracker.Clear();
        var (_, errors) = await _heatService.SaveHeatWithPositionsAsync(BuildHeat(swimEvent.Id, 2), isAdd: true);
        Assert.That(errors, Is.Empty);

        Context.ChangeTracker.Clear();
        var numbers = await GetHeatNumbersAsync(swimEvent.Id);
        Assert.That(numbers, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public async Task UpdateNumber_MovesHeatEarlier_ShiftsIntermediateHeatsDown()
    {
        var swimEvent = SeedEvent();
        var heat1 = await SeedHeatAsync(swimEvent.Id, 1);
        await SeedHeatAsync(swimEvent.Id, 2);
        await SeedHeatAsync(swimEvent.Id, 3);
        var heat4 = await SeedHeatAsync(swimEvent.Id, 4);

        var moved = await ReloadHeatAsync(heat4.Id);
        moved.Number = 2;
        var (_, errors) = await _heatService.SaveHeatWithPositionsAsync(moved, isAdd: false);
        Assert.That(errors, Is.Empty);

        var numbers = await GetHeatNumbersAsync(swimEvent.Id);
        Assert.That(numbers, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        Assert.That(await GetHeatNumberAsync(heat1.Id), Is.EqualTo(1));
        Assert.That(await GetHeatNumberAsync(heat4.Id), Is.EqualTo(2));
    }

    [Test]
    public async Task InsertAtFreeNumber_LeavesExistingHeatsUnchanged()
    {
        var swimEvent = SeedEvent();
        await SeedHeatAsync(swimEvent.Id, 1);
        await SeedHeatAsync(swimEvent.Id, 2);
        await SeedHeatAsync(swimEvent.Id, 4);

        Context.ChangeTracker.Clear();
        var (_, errors) = await _heatService.SaveHeatWithPositionsAsync(BuildHeat(swimEvent.Id, 5), isAdd: true);
        Assert.That(errors, Is.Empty);

        Context.ChangeTracker.Clear();
        var numbers = await GetHeatNumbersAsync(swimEvent.Id);
        Assert.That(numbers, Is.EqualTo(new[] { 1, 2, 4, 5 }));
    }

    [Test]
    public async Task UpdateNumber_MovesHeatToFreeNumber_LeavesOtherNumbersUnchanged()
    {
        var swimEvent = SeedEvent();
        await SeedHeatAsync(swimEvent.Id, 1);
        await SeedHeatAsync(swimEvent.Id, 3);
        await SeedHeatAsync(swimEvent.Id, 5);
        var heat = await SeedHeatAsync(swimEvent.Id, 10);

        var moved = await ReloadHeatAsync(heat.Id);
        moved.Number = 7;
        var (_, errors) = await _heatService.SaveHeatWithPositionsAsync(moved, isAdd: false);
        Assert.That(errors, Is.Empty);

        var numbers = await GetHeatNumbersAsync(swimEvent.Id);
        Assert.That(numbers, Is.EqualTo(new[] { 1, 3, 5, 7 }));
        Assert.That(await GetHeatNumberAsync(heat.Id), Is.EqualTo(7));
    }

    [Test]
    public async Task DeleteHeat_LeavesNumberGap()
    {
        var swimEvent = SeedEvent();
        await SeedHeatAsync(swimEvent.Id, 1);
        var deleted = await SeedHeatAsync(swimEvent.Id, 2);
        await SeedHeatAsync(swimEvent.Id, 3);

        await _heatService.DeleteHeatAsync(deleted.Id);

        var numbers = await GetHeatNumbersAsync(swimEvent.Id);
        Assert.That(numbers, Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public async Task InsertAndDelete_RecalculateGlobalHeatOrder()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var firstStyle = _seeder.SeedSwimStyle(50, Stroke.Free);
        var secondStyle = _seeder.SeedSwimStyle(100, Stroke.Free);
        var firstEvent = _seeder.SeedSwimEvent(ageGroup, firstStyle, order: 1);
        var secondEvent = _seeder.SeedSwimEvent(ageGroup, secondStyle, order: 2);
        await SeedHeatAsync(firstEvent.Id, 1);
        await SeedHeatAsync(firstEvent.Id, 2);
        await SeedHeatAsync(secondEvent.Id, 1);

        var orders = await GetGlobalHeatOrdersAsync();
        Assert.That(orders, Is.EqualTo(new[] { 1, 2, 3 }));

        var deleted = await Context.Heats
            .AsNoTracking()
            .Where(heat => heat.SwimEventId == firstEvent.Id && heat.Number == 1)
            .SingleAsync();
        await _heatService.DeleteHeatAsync(deleted.Id);

        orders = await GetGlobalHeatOrdersAsync();
        Assert.That(orders, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task SwimEventOrderChange_RecalculatesHeatOrders()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var firstStyle = _seeder.SeedSwimStyle(50, Stroke.Free);
        var secondStyle = _seeder.SeedSwimStyle(100, Stroke.Free);
        var firstEvent = _seeder.SeedSwimEvent(ageGroup, firstStyle, order: 1);
        var secondEvent = _seeder.SeedSwimEvent(ageGroup, secondStyle, order: 2);
        await SeedHeatAsync(firstEvent.Id, 1);
        await SeedHeatAsync(firstEvent.Id, 2);
        var secondEventHeat = await SeedHeatAsync(secondEvent.Id, 1);

        var moved = await Context.SwimEvents.AsNoTracking().SingleAsync(swimEvent => swimEvent.Id == secondEvent.Id);
        moved.Order = 1;
        var (_, errors) = await _eventService.UpdateAsync(moved);
        Assert.That(errors, Is.Empty);

        Assert.That(await GetHeatOrderAsync(secondEventHeat.Id), Is.EqualTo(1));
        var orders = await GetGlobalHeatOrdersAsync();
        Assert.That(orders, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    private SwimEvent SeedEvent()
    {
        var ageGroup = _seeder.SeedAgeGroup();
        var swimStyle = _seeder.SeedSwimStyle();
        return _seeder.SeedSwimEvent(ageGroup, swimStyle);
    }

    private async Task<Heat> SeedHeatAsync(int swimEventId, int number)
    {
        var (heat, errors) = await _heatService.SaveHeatWithPositionsAsync(BuildHeat(swimEventId, number), isAdd: true);
        Assert.That(errors, Is.Empty);
        return heat!;
    }

    private static Heat BuildHeat(int swimEventId, int number) =>
        new()
        {
            SwimEventId = swimEventId,
            Number = number,
            Status = HeatStatus.NOT_STARTED,
            Positions = []
        };

    private async Task<Heat> ReloadHeatAsync(int heatId) =>
        await Context.Heats.AsNoTracking().SingleAsync(heat => heat.Id == heatId);

    private async Task<int[]> GetHeatNumbersAsync(int swimEventId) =>
        await Context.Heats
            .AsNoTracking()
            .Where(heat => heat.SwimEventId == swimEventId)
            .OrderBy(heat => heat.Number)
            .ThenBy(heat => heat.Id)
            .Select(heat => heat.Number)
            .ToArrayAsync();

    private async Task<int> GetHeatNumberAsync(int heatId) =>
        await Context.Heats.AsNoTracking().Where(heat => heat.Id == heatId).Select(heat => heat.Number).SingleAsync();

    private async Task<int> GetHeatOrderAsync(int heatId) =>
        await Context.Heats.AsNoTracking().Where(heat => heat.Id == heatId).Select(heat => heat.Order).SingleAsync();

    private async Task<int[]> GetGlobalHeatOrdersAsync() =>
        await Context.Heats
            .AsNoTracking()
            .OrderBy(heat => heat.Order)
            .Select(heat => heat.Order)
            .ToArrayAsync();

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
