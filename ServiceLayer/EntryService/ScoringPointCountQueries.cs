using DataLayer.EfCore;
using DataLayer.Scoring;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.EntryService;

public static class ScoringPointCountQueries
{
    public static async Task<Dictionary<int, int>> GetAthletePointCountsAsync(
        EfCoreContext dbContext,
        IReadOnlyList<int> athleteIds,
        CancellationToken cancellationToken = default)
    {
        if (athleteIds.Count == 0)
            return [];

        var entries = await dbContext.Entries
            .AsNoTracking()
            .Where(e => e.AthleteId != null && athleteIds.Contains(e.AthleteId.Value) && e.Scoring)
            .Include(e => e.SwimEvent)
            .ToListAsync(cancellationToken);

        return entries
            .GroupBy(e => e.AthleteId!.Value)
            .ToDictionary(
                group => group.Key,
                group => ScoringPointsCalculator.CalculateAthleteScoringPoints(group));
    }

    public static async Task<Dictionary<int, int>> GetClubPointCountsAsync(
        EfCoreContext dbContext,
        IReadOnlyList<int> clubIds,
        CancellationToken cancellationToken = default)
    {
        if (clubIds.Count == 0)
            return [];

        var entries = await dbContext.Entries
            .AsNoTracking()
            .Where(e => e.AthleteId != null && e.Scoring)
            .Include(e => e.Athlete)
            .Include(e => e.SwimEvent)
            .Where(e => e.Athlete!.ClubId != null && clubIds.Contains(e.Athlete.ClubId.Value))
            .ToListAsync(cancellationToken);

        return entries
            .GroupBy(e => e.Athlete!.ClubId!.Value)
            .ToDictionary(
                clubGroup => clubGroup.Key,
                clubGroup => clubGroup
                    .GroupBy(e => e.AthleteId!.Value)
                    .Sum(athleteGroup => ScoringPointsCalculator.CalculateAthleteScoringPoints(athleteGroup)));
    }
}
