using System.ComponentModel.DataAnnotations;

namespace DataLayer.EfClasses;

public class Club : IValidatableObject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? ShortName { get; set; }
    public ClubType Type { get; set; } = ClubType.Unattached;

    public ICollection<Athlete> Athletes { get; set; }
    public ICollection<Relay> Relays { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name is null or "")
        {
            yield return new ValidationResult("Club name cannot be empty", [nameof(Name)]);
        }
    }
}