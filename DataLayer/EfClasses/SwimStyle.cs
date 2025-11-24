using System.ComponentModel.DataAnnotations;

namespace DataLayer.EfClasses;

public class SwimStyle : IValidatableObject
{
    public int Id { get; set; }
    public int RelayCount { get; set; } = 0;
    public required Stroke Stroke { get; set; }
    public required int Distance { get; set; }
    public bool IsRelay => RelayCount > 0;
    public bool IsIndividual => RelayCount == 0;
    public ICollection<SwimEvent> Events { get; set; }
    public ICollection<Entry> Entries { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Distance <= 0)
            yield return new ValidationResult("Distance must be greater than zero", [nameof(Distance)]);
    }
}