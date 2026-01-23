using System.ComponentModel.DataAnnotations;

namespace DataLayer.EfClasses;

public class Athlete : IValidatableObject
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required Gender Gender { get; set; }
    public required int YearOfBirth { get; set; }
    public Category Category { get; set; } = Category.NoCategory;

    public int? ClubId { get; set; }
    public Club? Club { get; set; }
    public ICollection<Entry> Entries { get; set; }
    public ICollection<RelayPosition> RelayPositions { get; set; }

    public string DisplayName => $"{FirstName} {LastName}";
    
    public string DisplayClubName => $"{Club?.Name ?? "(Лично)"}";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            yield return new ValidationResult("First name cannot be empty", [nameof(FirstName)]);
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            yield return new ValidationResult("Last name cannot be empty", [nameof(LastName)]);
        }

        if (Gender == Gender.Mixed)
        {
            yield return new ValidationResult("Gender cannot be mixed", [nameof(Gender)]);
        }

        if (YearOfBirth <= 1900)
        {
            yield return new ValidationResult("Year of birth is invalid", [nameof(YearOfBirth)]);
        }
    }
}