using System.ComponentModel.DataAnnotations;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfClasses;

public class SwimEvent : IValidatableObject
{
    public int Id { get; set; }
    public required int Order { get; set; }
    public required DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public EventRound Round { get; set; } = EventRound.FIN;
    public int? RoundParticipantsCount { get; set; }
    public required int LaneMin { get; set; }
    public required int LaneMax { get; set; }

    public int AgeGroupId { get; set; }
    public AgeGroup AgeGroup { get; set; }

    public int SwimStyleId { get; set; }
    public SwimStyle SwimStyle { get; set; }
    public bool IsRelay => SwimStyle.IsRelay;

    public ICollection<Heat> Heats { get; set; }
    public ICollection<Entry> Entries { get; set; }

    public int? PreviousSwimEventId { get; set; }
    public SwimEvent? PreviousSwimEvent { get; set; }
    public SwimEvent? NextSwimEvent { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;
        var existed = currContext.SwimEvents.FirstOrDefault(swimEvent => swimEvent.Order == Order && swimEvent.Date == Date);
        if (existed is not null)
            yield return new ValidationResult($"Swim event with order: {Order} on date: {Date} already exists", [nameof(Order)]);
        if (RoundParticipantsCount is <= 0)
            yield return new ValidationResult("Round participants count must be greater than zero", [nameof(RoundParticipantsCount)]);
        if (LaneMin > LaneMax)
            yield return new ValidationResult("Invalid lane range", [nameof(LaneMin), nameof(LaneMax)]);
        if (PreviousSwimEvent is not null && Round <= PreviousSwimEvent.Round)
            yield return new ValidationResult("Invalid round order", [nameof(Round)]);
    }
}