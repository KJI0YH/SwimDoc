using DataLayer.EfClasses;
using DataLayer.EfCore;

namespace Tests.TestInfrastructure;

public sealed class TestDataSeeder(EfCoreContext context)
{
    public AgeGroup SeedAgeGroup(
        Gender gender = Gender.Male,
        int birthYearMin = 2000,
        int birthYearMax = 2010,
        string? name = "Test age group")
    {
        var ageGroup = new AgeGroup
        {
            Name = name,
            Gender = gender,
            BirthYearMin = birthYearMin,
            BirthYearMax = birthYearMax
        };
        context.AgeGroups.Add(ageGroup);
        Assert.That(context.SaveChangesWithValidation(), Is.Empty);
        return ageGroup;
    }

    public SwimStyle SeedSwimStyle(int distance = 50, Stroke stroke = Stroke.Free)
    {
        var swimStyle = new SwimStyle { Distance = distance, Stroke = stroke };
        context.SwimStyles.Add(swimStyle);
        Assert.That(context.SaveChangesWithValidation(), Is.Empty);
        return swimStyle;
    }

    public SwimEvent SeedSwimEvent(
        AgeGroup ageGroup,
        SwimStyle swimStyle,
        int order = 1,
        int laneMin = 1,
        int laneMax = 6,
        EventRound round = EventRound.FIN)
    {
        var swimEvent = new SwimEvent
        {
            Order = order,
            Date = DateOnly.FromDateTime(DateTime.Today),
            LaneMin = laneMin,
            LaneMax = laneMax,
            RoundParticipantsCount = 8,
            AgeGroupId = ageGroup.Id,
            SwimStyleId = swimStyle.Id,
            Round = round
        };
        context.SwimEvents.Add(swimEvent);
        Assert.That(context.SaveChangesWithValidation(), Is.Empty);
        return swimEvent;
    }

    public (Club Club, Athlete Athlete, Entry Entry) SeedEntry(
        SwimEvent swimEvent,
        SwimStyle swimStyle,
        string firstName,
        string lastName,
        int yearOfBirth,
        Gender gender,
        int? entryTime,
        int? finishTime = null,
        int? points = null,
        EntryStatus status = EntryStatus.EVENT,
        string clubName = "Test club",
        Athlete? existingAthlete = null)
    {
        Club club;
        Athlete athlete;
        if (existingAthlete is not null)
        {
            athlete = existingAthlete;
            club = existingAthlete.Club ?? throw new InvalidOperationException("Athlete must belong to a club.");
        }
        else
        {
            club = new Club { Name = clubName };
            athlete = new Athlete
            {
                FirstName = firstName,
                LastName = lastName,
                YearOfBirth = yearOfBirth,
                Gender = gender,
                Club = club
            };
            context.Clubs.Add(club);
        }

        var entry = new Entry
        {
            Athlete = athlete,
            SwimStyleId = swimStyle.Id,
            SwimEventId = swimEvent.Id,
            SwimEvent = swimEvent,
            EntryTime = entryTime,
            FinishTime = finishTime,
            Points = points,
            Status = status,
            Scoring = true
        };
        context.Entries.Add(entry);
        context.NormalizeEntry(entry);
        Assert.That(context.SaveChangesWithValidation(), Is.Empty);
        return (club, athlete, entry);
    }
}
