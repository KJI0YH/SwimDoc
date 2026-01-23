using System.ComponentModel.DataAnnotations;
using DataLayer;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfClasses;

public class SwimStyle : IValidatableObject
{
    public int Id { get; set; }
    public int RelayCount { get; set; } = 0;
    public required Stroke Stroke { get; set; }
    public required int Distance { get; set; } = 50;
    public bool IsRelay => RelayCount > 0;
    public bool IsIndividual => RelayCount == 0;
    public ICollection<SwimEvent> Events { get; set; }
    public ICollection<Entry> Entries { get; set; }

    public string DisplayName =>
        $"{(IsRelay ? RelayCount + "x" : string.Empty)}{Distance}м {EnumDisplay.GetDescription(Stroke)}";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;
        var existed = currContext.SwimStyles.FirstOrDefault(ss =>
            ss.Stroke == Stroke && ss.Distance == Distance && ss.RelayCount == RelayCount);
        if (existed is not null && currContext.Entry(this).State == EntityState.Added)
            yield return new ValidationResult($"Swim style {DisplayName} already exists");
        if (Distance <= 0)
            yield return new ValidationResult("Distance must be greater than zero", [nameof(Distance)]);
    }
}