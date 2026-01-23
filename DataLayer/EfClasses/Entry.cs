using System.ComponentModel.DataAnnotations;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfClasses;

public class Entry : IValidatableObject
{
    public int Id { get; set; }
    public int? EntryTime { get; set; }
    public bool Scoring { get; set; } = true;
    public EntryStatus Status { get; set; } = EntryStatus.INS;
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

    public int? HeatPositionId { get; set; }
    public HeatPosition? HeatPosition { get; set; }

    public string DisplayEntryTime => EntryTime == null
        ? "N.T."
        : (EntryTime / 6000 == 0 ? "" : $"{EntryTime / 6000}:")
          + $"{((EntryTime % 6000) / 100):D2}."
          + $"{(EntryTime % 100):D2}";

    public string DisplayFinishTime => FinishTime == null
        ? "N.T."
        : (FinishTime / 6000 == 0 ? "" : $"{FinishTime / 6000}:")
          + $"{((FinishTime % 6000) / 100):D2}."
          + $"{(FinishTime % 100):D2}";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;
        var existed = currContext.Entries.FirstOrDefault(e =>
            e.AthleteId == AthleteId &&
            e.RelayId == RelayId &&
            e.SwimEventId == SwimEventId &&
            e.SwimStyleId == SwimStyleId);
        var athlete = currContext.Athletes.FirstOrDefault(a => a.Id == AthleteId);
        var relay = currContext.Relays.FirstOrDefault(r => r.Id == RelayId);
        var swimEvent = currContext.SwimEvents.Include(swimEvent => swimEvent.AgeGroup).FirstOrDefault(s => s.Id == SwimEventId);
        if (swimEvent is not null && SwimStyleId is 0)
        {
            SwimStyleId = swimEvent?.SwimStyleId ?? 0;
        }
        var swimStyle =  currContext.SwimStyles.FirstOrDefault(s => s.Id == SwimStyleId);
        
        if (AthleteId is null or 0 && RelayId is null or 0)
            yield return new ValidationResult("Participant must be provided");
        if (SwimStyleId is 0)
            yield return new ValidationResult("Swim style must be provided");
        if (existed is not null && currContext.Entry(this).State == EntityState.Added)
            yield return new ValidationResult($"Entry already exists");
        if (AthleteId is not null && RelayId is not null)
            yield return new ValidationResult("Invalid participant");
        if (athlete is not null && swimEvent is not null &&
            !swimEvent.AgeGroup.Contains(athlete.YearOfBirth, athlete.Gender))
            yield return new ValidationResult($"Athlete can not to be added to this age group");
        if (swimStyle is not null && swimStyle.IsRelay && athlete is not null)
            yield return new ValidationResult("Swim event must be individual");
        if (swimStyle is not null && swimStyle.IsIndividual && relay is not null)
            yield return new ValidationResult("Swim event must be relay");
        if (Status == EntryStatus.FIN && FinishTime == null)
            yield return new ValidationResult("Finish time is required");
        if (Status == EntryStatus.FIN && Points == null)
            yield return new ValidationResult("Points is required");
    }
}