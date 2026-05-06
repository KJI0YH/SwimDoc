using System.ComponentModel.DataAnnotations;
using DataLayer;
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

    public SwimEventStatus Status { get; set; } = SwimEventStatus.EMPTY;

    public Course Course { get; set; } = Course.LCM;
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

    public string DisplayName =>
        $"#{Order} {EnumDisplay.GetDescription(Round)} {SwimStyle.DisplayName} {AgeGroup.DisplayName}";

    public string DisplayLanes => $"{LaneMin}-{LaneMax}";

    public string DisplayDate => Date.ToShortDateString();

    public string DisplayTime => Time?.ToShortTimeString() ?? "Не назначено";

    public string DisplayStatus => Status.ToString();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;
        var existedOrderDate =
            currContext.SwimEvents.FirstOrDefault(swimEvent => swimEvent.Order == Order && swimEvent.Date == Date);
        var existedEvent = currContext.SwimEvents.FirstOrDefault(swimEvent =>
            swimEvent.SwimStyleId == SwimStyleId && swimEvent.AgeGroupId == AgeGroupId && swimEvent.Round == Round);
        if (existedOrderDate is not null && currContext.Entry(this).State == EntityState.Added)
            yield return new ValidationResult($"Swim event with order {Order} on date {Date} already exists");
        if (existedEvent is not null && currContext.Entry(this).State == EntityState.Added &&
            existedEvent.SwimStyleId == SwimStyleId &&
            existedEvent.AgeGroupId == AgeGroupId &&
            existedEvent.Round == Round)
            yield return new ValidationResult($"Swim event with this swim style, age group, round already exists");
        if (SwimStyleId is 0)
            yield return new ValidationResult($"Swim style is not selected");
        if (AgeGroupId is 0)
            yield return new ValidationResult($"Age group is not selected");
        if (RoundParticipantsCount is <= 0)
            yield return new ValidationResult("Round participants count must be greater than zero",
                [nameof(RoundParticipantsCount)]);
        if (LaneMin > LaneMax)
            yield return new ValidationResult("Invalid lane range");
        if (PreviousSwimEvent is not null && Round <= PreviousSwimEvent.Round)
            yield return new ValidationResult("Invalid round order");
    }
}