using System.ComponentModel.DataAnnotations;
using System.Globalization;
using DataLayer;
using DataLayer.EfCore;
using DataLayer.Resources;
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
    public string? CustomLaneNames { get; set; }
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
        var existedOrderDate =
            currContext.SwimEvents.FirstOrDefault(swimEvent => swimEvent.Order == Order && swimEvent.Date == Date);
        var existedEvent = currContext.SwimEvents.FirstOrDefault(swimEvent =>
            swimEvent.SwimStyleId == SwimStyleId && swimEvent.AgeGroupId == AgeGroupId && swimEvent.Round == Round);
        if (existedOrderDate is not null && currContext.Entry(this).State == EntityState.Added)
            yield return new ValidationResult(string.Format(
                CultureInfo.CurrentUICulture,
                ValidationStrings.SwimEvent_OrderAndDateAlreadyExists_Format,
                Order,
                Date));
        if (existedEvent is not null && currContext.Entry(this).State == EntityState.Added &&
            existedEvent.SwimStyleId == SwimStyleId &&
            existedEvent.AgeGroupId == AgeGroupId &&
            existedEvent.Round == Round)
            yield return new ValidationResult(ValidationStrings.SwimEvent_AlreadyExists_ByStyleAgeGroupRound);
        if (SwimStyleId is 0)
            yield return new ValidationResult(ValidationStrings.SwimEvent_SwimStyleNotSelected);
        if (AgeGroupId is 0)
            yield return new ValidationResult(ValidationStrings.SwimEvent_AgeGroupNotSelected);
        if (RoundParticipantsCount is <= 0)
            yield return new ValidationResult(
                ValidationStrings.SwimEvent_RoundParticipantsCountMustBeGreaterThanZero,
                [nameof(RoundParticipantsCount)]);
        var customLanes = SwimEventLaneNames.Parse(CustomLaneNames);
        if (customLanes.Count > 0)
        {
            if (customLanes.Count != customLanes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                yield return new ValidationResult(
                    ValidationStrings.SwimEvent_CustomLaneNamesMustBeUnique,
                    [nameof(CustomLaneNames)]);
        }
        else if (LaneMin > LaneMax)
        {
            yield return new ValidationResult(ValidationStrings.SwimEvent_InvalidLaneRange);
        }
        if (PreviousSwimEvent is not null && Round <= PreviousSwimEvent.Round)
            yield return new ValidationResult(ValidationStrings.SwimEvent_InvalidRoundOrder);
    }
}
