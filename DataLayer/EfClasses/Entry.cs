using System.ComponentModel.DataAnnotations;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfClasses;

public class Entry : IValidatableObject
{
    public int Id { get; set; }
    public int? EntryTime { get; set; }
    public bool Scoring { get; set; } = true;
    public EntryStatus Status { get; set; } = EntryStatus.ENTRY;
    public int SwimStyleId { get; set; }
    public SwimStyle SwimStyle { get; set; }
    public string? Comment { get; set; }
    public int? FinishTime { get; set; }
    public int? Points { get; set; }

    public int? AthleteId { get; set; }
    public Athlete? Athlete { get; set; }

    public int? RelayId { get; set; }
    public Relay? Relay { get; set; }

    public int? SwimEventId { get; set; }
    public SwimEvent? SwimEvent { get; set; }
    public HeatPosition? HeatPosition { get; set; }

    public string DisplaySwimName => SwimEvent is not null ? SwimEvent.DisplayName : SwimStyle.DisplayName;

    public string DisplayParticipantName => Athlete is not null
        ? Athlete.DisplayName
        : Relay is not null
            ? Relay.DisplayNameWithAthletes
            : string.Empty;

    public string DisplayParticipantClubName => Athlete is not null
        ? Athlete.DisplayClubName
        : Relay is not null
            ? Relay.Club.Name
            : string.Empty;

    public string DisplayEntryTime => EntryTime == null
        ? "N.T."
        : (EntryTime / 6000 == 0 ? "" : $"{EntryTime / 6000}:")
          + $"{((EntryTime % 6000) / 100):D2}."
          + $"{(EntryTime % 100):D2}";

    public string DisplayFinishTime => Status switch
    {
        EntryStatus.FINISH => (FinishTime / 6000 == 0 ? "" : $"{FinishTime / 6000}:")
                              + $"{((FinishTime % 6000) / 100):D2}."
                              + $"{(FinishTime % 100):D2}",
        EntryStatus.DNF or EntryStatus.DNS or EntryStatus.DSQ => Status.ToString(),
        _ => ""
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;

        var existed = currContext.Entries.AsNoTracking().FirstOrDefault(e =>
            e.AthleteId == AthleteId &&
            e.RelayId == RelayId &&
            e.SwimEventId == SwimEventId &&
            e.SwimStyleId == SwimStyleId);
        var athlete = Athlete ?? currContext.Athletes.AsNoTracking().FirstOrDefault(a => a.Id == AthleteId);
        var relay = Relay ?? currContext.Relays.AsNoTracking()
            .Include(r => r.Positions)
            .ThenInclude(p => p.Athlete)
            .FirstOrDefault(r => r.Id == RelayId);
        var relayAthletes = relay is not null
            ? currContext.Athletes.Where(a => relay.Positions.Select(p => p.AthleteId).Contains(a.Id)).ToList()
            : [];
        var swimEvent = SwimEvent ?? currContext.SwimEvents.AsNoTracking()
            .Include(swimEvent => swimEvent.AgeGroup)
            .FirstOrDefault(s => s.Id == SwimEventId);
        var swimStyle = SwimStyle ?? currContext.SwimStyles.AsNoTracking().FirstOrDefault(s => s.Id == SwimStyleId);
        var ageGroup = swimEvent?.AgeGroup ?? (swimEvent is null
            ? null
            : currContext.AgeGroups.AsNoTracking()
                .FirstOrDefault(ag => ag.Id == swimEvent.AgeGroupId));


        if (athlete is null && relay is null)
            yield return new ValidationResult("Participant must be provided");
        if (swimStyle is null)
            yield return new ValidationResult("Swim style must be provided");
        if (existed is not null && currContext.Entry(this).State == EntityState.Added)
            yield return new ValidationResult($"Entry already exists");
        if (athlete is not null && relay is not null)
            yield return new ValidationResult("Invalid participant");
        if (athlete is not null && swimEvent is not null && !ageGroup.Contains(athlete.YearOfBirth, athlete.Gender) ||
            relay is not null && swimEvent is not null &&
            relayAthletes.Any(a => !ageGroup.Contains(a.YearOfBirth, a.Gender)))
            yield return new ValidationResult($"Athlete can not to be added to this age group");
        if (swimStyle is not null && swimStyle.IsRelay && athlete is not null)
            yield return new ValidationResult("Swim event must be individual");
        if (swimStyle is not null && swimStyle.IsIndividual && relay is not null)
            yield return new ValidationResult("Swim event must be relay");
    }
}