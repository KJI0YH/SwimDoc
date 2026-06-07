using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace UI.Models.Rows.Projections;

public static class EntryRelayPositionEnricher
{
    public static async Task EnrichAsync(IReadOnlyList<EntryRowProjection> projections, EfCoreContext db)
    {
        var relayIds = projections
            .Where(projection => projection.RelayId.HasValue)
            .Select(projection => projection.RelayId!.Value)
            .Distinct()
            .ToList();
        if (relayIds.Count == 0)
            return;
        var positions = await db.Set<RelayPosition>()
            .Where(position => relayIds.Contains(position.RelayId))
            .OrderBy(position => position.Order)
            .Select(position => new RelayPositionRowProjection
            {
                RelayId = position.RelayId,
                Order = position.Order,
                AthleteFirstName = position.Athlete.FirstName,
                AthleteLastName = position.Athlete.LastName,
                AthleteYearOfBirth = position.Athlete.YearOfBirth
            })
            .ToListAsync();
        var positionsByRelay = positions.GroupBy(position => position.RelayId)
            .ToDictionary(group => group.Key, group => group.ToList());
        foreach (var projection in projections.Where(projection => projection.RelayId.HasValue))
        {
            if (positionsByRelay.TryGetValue(projection.RelayId!.Value, out var relayPositions))
                projection.RelayPositions = relayPositions;
        }
    }
}
