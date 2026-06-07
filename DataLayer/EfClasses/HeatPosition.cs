using System.ComponentModel.DataAnnotations;
using System.Globalization;
using DataLayer;
using DataLayer.Display;
using DataLayer.EfCore;
using DataLayer.Resources;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfClasses;

public class HeatPosition : IValidatableObject
{
    public int HeatId { get; set; }
    public int Lane { get; set; }
    public int EntryId { get; set; }

    public Entry Entry { get; set; }
    public Heat Heat { get; set; }

    public bool IsResultProvided => Entry.Status >= EntryStatus.FINISH;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;
        var heat = currContext.Heats.Include(heat => heat.SwimEvent).FirstOrDefault(heat => heat.Id == HeatId);
        if (heat is null)
            yield return new ValidationResult(ValidationStrings.HeatPosition_HeatDoesNotExist, [nameof(HeatId)]);
        else if (!SwimEventLaneNames.IsLaneInRange(heat.SwimEvent, Lane))
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    ValidationStrings.HeatPosition_LaneOutOfRange_Format,
                    SwimEventLaneNames.GetLaneDisplay(heat.SwimEvent, Lane)),
                [nameof(Lane)]);
        var existedLane = currContext.HeatPositions.FirstOrDefault(pos => pos.HeatId == HeatId && pos.Lane == Lane);
        if (existedLane is not null)
            yield return new ValidationResult(string.Format(
                CultureInfo.CurrentUICulture,
                ValidationStrings.HeatPosition_LaneAlreadyBusy_Format,
                Lane,
                HeatId), [nameof(Lane), nameof(HeatId)]);
        var existedEntry = currContext.HeatPositions.FirstOrDefault(pos => pos.EntryId == EntryId);
        if (existedEntry is not null)
            yield return new ValidationResult(string.Format(
                CultureInfo.CurrentUICulture,
                ValidationStrings.HeatPosition_EntryAlreadyExists_Format,
                EntryId), [nameof(EntryId)]);
    }
}
