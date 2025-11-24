using System.ComponentModel.DataAnnotations;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfClasses;

public class HeatPosition : IValidatableObject
{
    public int HeatId { get; set; }
    public int Lane { get; set; }
    public int EntryId { get; set; }

    public Entry Entry { get; set; }
    public Heat Heat { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;
        var heat = currContext.Heats.Include(heat => heat.SwimEvent).FirstOrDefault(heat => heat.Id == HeatId);
        if (heat is null)
            yield return new ValidationResult("Heat does not exist", [nameof(HeatId)]);
        else if (heat.SwimEvent.LaneMin < Lane || heat.SwimEvent.LaneMax > Lane)
            yield return new ValidationResult($"Lane {Lane} is out of range", [nameof(Lane)]);
        var existedLane = currContext.HeatPositions.FirstOrDefault(pos => pos.HeatId == HeatId && pos.Lane == Lane);
        if (existedLane is not null)
            yield return new ValidationResult($"Lane: {Lane} in heat with ID: {HeatId} already busy", [nameof(Lane), nameof(HeatId)]);
        var existedEntry = currContext.HeatPositions.FirstOrDefault(pos => pos.EntryId == EntryId);
        if (existedEntry is not null)
            yield return new ValidationResult($"Entry with ID: {EntryId}  already exists", [nameof(EntryId)]);
    }
}