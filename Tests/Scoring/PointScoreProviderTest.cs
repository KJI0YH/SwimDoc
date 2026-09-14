using DataLayer.EfClasses;
using NUnit.Framework;
using ServiceLayer.BaseTimeRepository;
using ServiceLayer.PointScoreProvider;
using ServiceLayer.Scoring;

namespace Tests.Scoring;

[TestFixture]
public sealed class PointScoreProviderTest
{
    [Test]
    public void CalculatePoints_WorldAquatics_UsesClassicFormula()
    {
        var settings = new FakeScoringSettings(new ActiveScoringSettings
        {
            Mode = ScoringMode.WorldAquatics,
            PlacePoints = [9, 7, 6]
        });
        var baseTimes = new FakeBaseTimeRepository(1000);
        var provider = new PointScoreProvider(baseTimes, settings);

        Assert.That(provider.CalculatePoints(Course.LCM, 50, Stroke.Free, 0, Gender.Male, 1000), Is.EqualTo(1000));
        Assert.That(provider.CalculatePoints(Course.LCM, 50, Stroke.Free, 0, Gender.Male, 2000), Is.EqualTo(125));
    }

    [Test]
    public void ApplyEventPoints_PlaceTable_AssignsPointsByPlace()
    {
        var settings = new FakeScoringSettings(new ActiveScoringSettings
        {
            Mode = ScoringMode.PlaceTable,
            PlacePoints = [9, 7, 6, 5]
        });
        var provider = new PointScoreProvider(new FakeBaseTimeRepository(1000), settings);
        var swimEvent = CreateSwimEvent();
        var entries = new List<Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 2100),
            CreateEntry(2, EntryStatus.FINISH, 2000),
            CreateEntry(3, EntryStatus.FINISH, 2000),
            CreateEntry(4, EntryStatus.DNS, null),
        };

        provider.ApplyEventPoints(swimEvent, entries);

        Assert.That(entries.Single(e => e.Id == 2).Points, Is.EqualTo(9));
        Assert.That(entries.Single(e => e.Id == 3).Points, Is.EqualTo(9));
        Assert.That(entries.Single(e => e.Id == 1).Points, Is.EqualTo(6));
        Assert.That(entries.Single(e => e.Id == 4).Points, Is.EqualTo(0));
    }

    [Test]
    public void ApplyEventPoints_PlaceTable_RanksCurrentHeatAgainstFullEvent()
    {
        var settings = new FakeScoringSettings(new ActiveScoringSettings
        {
            Mode = ScoringMode.PlaceTable,
            PlacePoints = [50, 45, 40, 36]
        });
        var provider = new PointScoreProvider(new FakeBaseTimeRepository(1000), settings);
        var swimEvent = CreateSwimEvent();
        var alreadyFinished = new List<Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 1900),
            CreateEntry(2, EntryStatus.FINISH, 2000),
            CreateEntry(10, EntryStatus.ENTRY, null),
            CreateEntry(11, EntryStatus.FINISH, 2100),
        };
        var currentHeat = new List<Entry>
        {
            CreateEntry(10, EntryStatus.FINISH, 1850),
            CreateEntry(11, EntryStatus.FINISH, 1950),
        };

        provider.ApplyEventPoints(swimEvent, currentHeat, alreadyFinished);

        Assert.That(currentHeat.Single(e => e.Id == 10).Points, Is.EqualTo(50));
        Assert.That(currentHeat.Single(e => e.Id == 11).Points, Is.EqualTo(40));
        Assert.That(alreadyFinished.Single(e => e.Id == 1).Points, Is.EqualTo(45));
        Assert.That(alreadyFinished.Single(e => e.Id == 2).Points, Is.EqualTo(36));
    }

    private static SwimEvent CreateSwimEvent() =>
        new()
        {
            Id = 1,
            Order = 1,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Course = Course.LCM,
            LaneMin = 1,
            LaneMax = 8,
            SwimStyle = new SwimStyle { Distance = 50, Stroke = Stroke.Free, RelayCount = 0 },
            AgeGroup = new AgeGroup { Gender = Gender.Male }
        };

    private static Entry CreateEntry(int id, EntryStatus status, int? finishTime) =>
        new()
        {
            Id = id,
            Status = status,
            FinishTime = finishTime,
            SwimStyleId = 1,
            SwimStyle = new SwimStyle { Distance = 50, Stroke = Stroke.Free, RelayCount = 0 }
        };

    private sealed class FakeScoringSettings(ActiveScoringSettings current) : IScoringSettingsService
    {
        public ActiveScoringSettings Current { get; private set; } = current;
#pragma warning disable CS0067
        public event Action? Changed;
#pragma warning restore CS0067
        public void SetActive(ActiveScoringSettings settings, bool raiseChanged = true) => Current = settings;
    }

    private sealed class FakeBaseTimeRepository(int baseTime) : IBaseTimeRepository
    {
        public int GetBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex) => baseTime;
        public void SetBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex, int hundredths) { }
        public void Save() { }
    }
}
