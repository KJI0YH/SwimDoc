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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;
        if (Athlete is null && Relay is null)
            yield return new ValidationResult("Participant must be provided");
        if (Athlete is not null && Relay is not null)
            yield return new ValidationResult("Invalid participant", [nameof(Athlete), nameof(Relay)]);
        if (SwimStyle.IsRelay && Athlete is not null)
            yield return new ValidationResult("Swim event must be individual");
        if (SwimStyle.IsIndividual && Relay is not null)
            yield return new ValidationResult("Swim event must be relay");
        if (Status == EntryStatus.FIN && FinishTime == null)
            yield return new ValidationResult("Finish time is required");
        if (Status == EntryStatus.FIN && Points == null)
            yield return new ValidationResult("Points is required");
    }
}