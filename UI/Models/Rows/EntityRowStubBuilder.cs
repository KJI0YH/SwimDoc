using DataLayer.EfClasses;
using UI.Models.Rows.Projections;

namespace UI.Models.Rows;

internal static class EntityRowStubBuilder
{
    public static Entry BuildEntry(EntryRowProjection p)
    {
        var entry = new Entry
        {
            Id = p.Id,
            Scoring = p.Scoring,
            Status = p.Status,
            EntryTime = p.EntryTime,
            FinishTime = p.FinishTime,
            Points = p.Points,
            Comment = p.Comment,
            AthleteId = p.AthleteId,
            RelayId = p.RelayId,
            SwimEventId = p.SwimEventId,
            SwimStyleId = p.SwimStyleId,
            SwimStyle = BuildSwimStyle(p.SwimStyleId, p.SwimStyleDistance, p.SwimStyleStroke, p.SwimStyleRelayCount,
                p.SwimStyleIsRelay),
            HeatPosition = p.HasHeatPosition ? new HeatPosition() : null
        };
        if (p.AthleteId is int athleteId)
        {
            entry.Athlete = new Athlete
            {
                Id = athleteId,
                FirstName = p.AthleteFirstName ?? string.Empty,
                LastName = p.AthleteLastName ?? string.Empty,
                Gender = Gender.Mixed,
                YearOfBirth = p.AthleteYearOfBirth ?? 0,
                Category = p.AthleteCategory ?? Category.NoCategory,
                Club = p.AthleteClubName is not null ? new Club { Name = p.AthleteClubName } : null
            };
        }

        if (p.RelayId is int relayId)
        {
            entry.Relay = new Relay
            {
                Id = relayId,
                Number = p.RelayNumber,
                Club = p.RelayClubName is not null ? new Club { Name = p.RelayClubName } : new Club(),
                Positions = p.RelayPositions
                    .OrderBy(pos => pos.Order)
                    .Select(pos => new RelayPosition
                    {
                        RelayId = pos.RelayId,
                        Order = pos.Order,
                        Athlete = new Athlete
                        {
                            FirstName = pos.AthleteFirstName ?? string.Empty,
                            LastName = pos.AthleteLastName ?? string.Empty,
                            Gender = Gender.Mixed,
                            YearOfBirth = pos.AthleteYearOfBirth ?? 0
                        }
                    })
                    .ToList()
            };
        }

        if (p.SwimEventOrder is not null)
        {
            entry.SwimEvent = BuildSwimEvent(new SwimEventRowProjection
            {
                Id = p.SwimEventId ?? 0,
                Order = p.SwimEventOrder.Value,
                Date = default,
                Round = p.SwimEventRound ?? EventRound.FIN,
                LaneMin = 0,
                LaneMax = 0,
                Status = SwimEventStatus.EMPTY,
                SwimStyleId = p.SwimStyleId,
                SwimStyleDistance = p.SwimStyleDistance,
                SwimStyleStroke = p.SwimStyleStroke,
                SwimStyleRelayCount = p.SwimStyleRelayCount,
                SwimStyleIsRelay = p.SwimStyleIsRelay,
                AgeGroupId = p.SwimEventAgeGroupId ?? 0,
                AgeGroupName = p.SwimEventAgeGroupName,
                AgeGroupGender = p.SwimEventAgeGroupGender ?? Gender.Mixed,
                AgeGroupBirthYearMin = p.SwimEventAgeGroupBirthYearMin,
                AgeGroupBirthYearMax = p.SwimEventAgeGroupBirthYearMax
            });
        }

        return entry;
    }

    public static SwimEvent BuildSwimEvent(SwimEventRowProjection p) =>
        new()
        {
            Id = p.Id,
            Order = p.Order,
            Course = p.Course,
            Date = p.Date,
            Time = p.Time,
            Round = p.Round,
            LaneMin = p.LaneMin,
            LaneMax = p.LaneMax,
            CustomLaneNames = p.CustomLaneNames,
            Status = p.Status,
            AgeGroupId = p.AgeGroupId,
            AgeGroup = BuildAgeGroup(p.AgeGroupId, p.AgeGroupName, p.AgeGroupGender, p.AgeGroupBirthYearMin,
                p.AgeGroupBirthYearMax),
            SwimStyleId = p.SwimStyleId,
            SwimStyle = BuildSwimStyle(p.SwimStyleId, p.SwimStyleDistance, p.SwimStyleStroke, p.SwimStyleRelayCount,
                p.SwimStyleIsRelay)
        };

    public static Athlete BuildAthlete(AthleteRowProjection p) =>
        new()
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            YearOfBirth = p.YearOfBirth,
            Gender = p.Gender,
            Category = p.Category,
            Club = p.ClubName is not null ? new Club { Name = p.ClubName } : null
        };

    public static Club BuildClub(ClubRowProjection p) =>
        new()
        {
            Id = p.Id,
            Name = p.Name
        };

    public static AgeGroup BuildAgeGroup(AgeGroupRowProjection p) =>
        BuildAgeGroup(p.Id, p.Name, p.Gender, p.BirthYearMin, p.BirthYearMax);

    public static SwimStyle BuildSwimStyle(SwimStyleRowProjection p) =>
        BuildSwimStyle(p.Id, p.Distance, p.Stroke, p.RelayCount, p.IsRelay);

    private static AgeGroup BuildAgeGroup(int id, string? name, Gender gender, int? birthYearMin, int? birthYearMax) =>
        new()
        {
            Id = id,
            Name = name,
            Gender = gender,
            BirthYearMin = birthYearMin,
            BirthYearMax = birthYearMax
        };

    private static SwimStyle BuildSwimStyle(int id, int distance, Stroke stroke, int relayCount, bool _) =>
        new()
        {
            Id = id,
            Distance = distance,
            Stroke = stroke,
            RelayCount = relayCount
        };
}
