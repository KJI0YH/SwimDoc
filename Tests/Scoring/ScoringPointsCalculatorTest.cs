using DataLayer.EfClasses;
using DataLayer.Scoring;

namespace Tests.Scoring;

[TestFixture]
public class ScoringPointsCalculatorTest
{
    [Test]
    public void CalculateAthleteScoringPoints_UsesHighestRoundOnlyPerDistance()
    {
        const int swimStyleId = 10;
        const int ageGroupId = 1;
        var preEvent = CreateEvent(1, swimStyleId, ageGroupId, EventRound.PRE);
        var semEvent = CreateEvent(2, swimStyleId, ageGroupId, EventRound.SEM);
        var finEvent = CreateEvent(3, swimStyleId, ageGroupId, EventRound.FIN);

        var entries = new[]
        {
            CreateEntry(preEvent, points: 12, scoring: true),
            CreateEntry(semEvent, points: 10, scoring: true),
            CreateEntry(finEvent, points: 8, scoring: true)
        };

        Assert.That(ScoringPointsCalculator.CalculateAthleteScoringPoints(entries), Is.EqualTo(8));
    }

    [Test]
    public void CalculateAthleteScoringPoints_SumsDifferentDistancesIndependently()
    {
        var freeEvent = CreateEvent(1, swimStyleId: 10, ageGroupId: 1, EventRound.FIN);
        var backPre = CreateEvent(2, swimStyleId: 20, ageGroupId: 1, EventRound.PRE);
        var backFin = CreateEvent(3, swimStyleId: 20, ageGroupId: 1, EventRound.FIN);

        var entries = new[]
        {
            CreateEntry(freeEvent, points: 15, scoring: true),
            CreateEntry(backPre, points: 11, scoring: true),
            CreateEntry(backFin, points: 9, scoring: true)
        };

        Assert.That(ScoringPointsCalculator.CalculateAthleteScoringPoints(entries), Is.EqualTo(24));
    }

    [Test]
    public void CalculateAthleteScoringPoints_KeepsSameDistanceInDifferentAgeGroupsSeparate()
    {
        var juniorFin = CreateEvent(1, swimStyleId: 10, ageGroupId: 1, EventRound.FIN);
        var seniorFin = CreateEvent(2, swimStyleId: 10, ageGroupId: 2, EventRound.FIN);

        var entries = new[]
        {
            CreateEntry(juniorFin, points: 7, scoring: true),
            CreateEntry(seniorFin, points: 5, scoring: true)
        };

        Assert.That(ScoringPointsCalculator.CalculateAthleteScoringPoints(entries), Is.EqualTo(12));
    }

    [Test]
    public void CalculateAthleteScoringPoints_IgnoresNonScoringEntries()
    {
        var preEvent = CreateEvent(1, swimStyleId: 10, ageGroupId: 1, EventRound.PRE);
        var finEvent = CreateEvent(2, swimStyleId: 10, ageGroupId: 1, EventRound.FIN);

        var entries = new[]
        {
            CreateEntry(preEvent, points: 12, scoring: true),
            CreateEntry(finEvent, points: 8, scoring: false)
        };

        Assert.That(ScoringPointsCalculator.CalculateAthleteScoringPoints(entries), Is.EqualTo(12));
    }

    private static SwimEvent CreateEvent(int id, int swimStyleId, int ageGroupId, EventRound round) =>
        new()
        {
            Id = id,
            Order = id,
            Date = new DateOnly(2026, 1, 1),
            LaneMin = 0,
            LaneMax = 7,
            SwimStyleId = swimStyleId,
            AgeGroupId = ageGroupId,
            Round = round,
            SwimStyle = new SwimStyle
            {
                Id = swimStyleId,
                Stroke = Stroke.Free,
                Distance = 100
            },
            AgeGroup = new AgeGroup
            {
                Id = ageGroupId,
                Name = $"AG-{ageGroupId}",
                Gender = Gender.Mixed,
                BirthYearMin = 2000,
                BirthYearMax = 2010
            }
        };

    private static Entry CreateEntry(SwimEvent swimEvent, int points, bool scoring) =>
        new()
        {
            SwimEventId = swimEvent.Id,
            SwimEvent = swimEvent,
            SwimStyleId = swimEvent.SwimStyleId,
            Scoring = scoring,
            Points = points
        };
}
