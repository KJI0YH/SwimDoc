using DataLayer.EfClasses;

namespace UI.Models.Rows.Projections;

public static class RowProjectionQueries
{
    public static IQueryable<EntryRowProjection> SelectEntry(IQueryable<Entry> query) =>
        query.Select(e => new EntryRowProjection
        {
            Id = e.Id,
            Scoring = e.Scoring,
            Status = e.Status,
            EntryTime = e.EntryTime,
            FinishTime = e.FinishTime,
            Points = e.Points,
            Comment = e.Comment,
            AthleteId = e.AthleteId,
            RelayId = e.RelayId,
            SwimEventId = e.SwimEventId,
            HasHeatPosition = e.HeatPosition != null,
            AthleteFirstName = e.Athlete != null ? e.Athlete.FirstName : null,
            AthleteLastName = e.Athlete != null ? e.Athlete.LastName : null,
            AthleteYearOfBirth = e.Athlete != null ? e.Athlete.YearOfBirth : null,
            AthleteCategory = e.Athlete != null ? e.Athlete.Category : null,
            AthleteClubName = e.Athlete != null && e.Athlete.Club != null ? e.Athlete.Club.Name : null,
            RelayClubName = e.Relay != null && e.Relay.Club != null ? e.Relay.Club.Name : null,
            RelayNumber = e.Relay != null ? e.Relay.Number : null,
            SwimStyleId = e.SwimStyleId,
            SwimStyleDistance = e.SwimStyle.Distance,
            SwimStyleStroke = e.SwimStyle.Stroke,
            SwimStyleRelayCount = e.SwimStyle.RelayCount,
            SwimStyleIsRelay = e.SwimStyle.IsRelay,
            SwimEventOrder = e.SwimEvent != null ? e.SwimEvent.Order : null,
            SwimEventRound = e.SwimEvent != null ? e.SwimEvent.Round : null,
            SwimEventAgeGroupId = e.SwimEvent != null ? e.SwimEvent.AgeGroupId : null,
            SwimEventAgeGroupName = e.SwimEvent != null && e.SwimEvent.AgeGroup != null
                ? e.SwimEvent.AgeGroup.Name
                : null,
            SwimEventAgeGroupGender = e.SwimEvent != null && e.SwimEvent.AgeGroup != null
                ? e.SwimEvent.AgeGroup.Gender
                : null,
            SwimEventAgeGroupBirthYearMin = e.SwimEvent != null && e.SwimEvent.AgeGroup != null
                ? e.SwimEvent.AgeGroup.BirthYearMin
                : null,
            SwimEventAgeGroupBirthYearMax = e.SwimEvent != null && e.SwimEvent.AgeGroup != null
                ? e.SwimEvent.AgeGroup.BirthYearMax
                : null
        });

    public static IQueryable<SwimEventRowProjection> SelectSwimEvent(IQueryable<SwimEvent> query) =>
        query.Select(se => new SwimEventRowProjection
        {
            Id = se.Id,
            Order = se.Order,
            Course = se.Course,
            Date = se.Date,
            Time = se.Time,
            Round = se.Round,
            LaneMin = se.LaneMin,
            LaneMax = se.LaneMax,
            CustomLaneNames = se.CustomLaneNames,
            Status = se.Status,
            SwimStyleId = se.SwimStyleId,
            SwimStyleDistance = se.SwimStyle.Distance,
            SwimStyleStroke = se.SwimStyle.Stroke,
            SwimStyleRelayCount = se.SwimStyle.RelayCount,
            SwimStyleIsRelay = se.SwimStyle.IsRelay,
            AgeGroupId = se.AgeGroupId,
            AgeGroupName = se.AgeGroup.Name,
            AgeGroupGender = se.AgeGroup.Gender,
            AgeGroupBirthYearMin = se.AgeGroup.BirthYearMin,
            AgeGroupBirthYearMax = se.AgeGroup.BirthYearMax,
            RoundParticipantsCount = se.RoundParticipantsCount,
            EntryCount = se.Entries.Count,
            HeatCount = se.Heats.Count
        });

    public static IQueryable<AthleteRowProjection> SelectAthlete(IQueryable<Athlete> query) =>
        query.Select(a => new AthleteRowProjection
        {
            Id = a.Id,
            FirstName = a.FirstName,
            LastName = a.LastName,
            YearOfBirth = a.YearOfBirth,
            Gender = a.Gender,
            Category = a.Category,
            ClubName = a.Club != null ? a.Club.Name : null,
            PointCount = a.Entries.Where(e => e.Scoring).Sum(e => e.Points ?? 0)
        });

    public static IQueryable<ClubRowProjection> SelectClub(IQueryable<Club> query) =>
        query.Select(c => new ClubRowProjection
        {
            Id = c.Id,
            Name = c.Name,
            AthleteCount = c.Athletes.Count,
            EntryScoringCount = c.Athletes.Sum(a => a.Entries.Count(e => e.Scoring)),
            EntryPersonalCount = c.Athletes.Sum(a => a.Entries.Count(e => !e.Scoring)),
            RelayScoringCount = c.Relays.Count(r => r.Entry != null && r.Entry.Scoring),
            RelayPersonalCount = c.Relays.Count(r => r.Entry != null && !r.Entry.Scoring),
            PointCount = c.Athletes.Sum(a => a.Entries.Where(e => e.Scoring).Sum(e => e.Points ?? 0))
        });

    public static IQueryable<AgeGroupRowProjection> SelectAgeGroup(
        IQueryable<AgeGroup> query,
        IQueryable<Athlete> athletes) =>
        query.Select(ag => new AgeGroupRowProjection
        {
            Id = ag.Id,
            Name = ag.Name,
            Gender = ag.Gender,
            BirthYearMin = ag.BirthYearMin,
            BirthYearMax = ag.BirthYearMax,
            ParticipantCount = athletes.Count(a =>
                a.YearOfBirth >= (ag.BirthYearMin ?? 0) &&
                a.YearOfBirth <= (ag.BirthYearMax ?? int.MaxValue) &&
                (ag.Gender == Gender.Mixed || a.Gender == ag.Gender))
        });

    public static IQueryable<SwimStyleRowProjection> SelectSwimStyle(IQueryable<SwimStyle> query) =>
        query.Select(ss => new SwimStyleRowProjection
        {
            Id = ss.Id,
            Distance = ss.Distance,
            Stroke = ss.Stroke,
            RelayCount = ss.RelayCount,
            IsRelay = ss.IsRelay
        });
}
