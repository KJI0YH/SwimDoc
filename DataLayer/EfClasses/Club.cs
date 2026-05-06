using System.ComponentModel.DataAnnotations;

namespace DataLayer.EfClasses;

public class Club : IValidatableObject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? ShortName { get; set; }

    public ICollection<Athlete> Athletes { get; set; }
    public ICollection<Relay> Relays { get; set; }
    
    public int DisplayAthleteCount => Athletes.Count;
    
    public int DisplayRelayCount => Relays.Count;
    
    public int DisplayEntryCount => Athletes.Sum(a => a.Entries.Count);

    public int DisplayPointCount => Athletes.Sum(a => a.Entries.Where(e => e.Scoring).Sum(e => e.Points ?? 0));

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name is null or "")
        {
            yield return new ValidationResult("Club name cannot be empty", [nameof(Name)]);
        }
    }
}