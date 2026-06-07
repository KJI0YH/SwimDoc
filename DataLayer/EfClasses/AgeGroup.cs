using System.ComponentModel.DataAnnotations;
using DataLayer;
using DataLayer.EfCore;
using DataLayer.Resources;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfClasses;

public class AgeGroup : IValidatableObject
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Gender Gender { get; set; }
    public int? BirthYearMin { get; set; }
    public int? BirthYearMax { get; set; }

    public ICollection<SwimEvent> Events { get; set; }

    public bool Contains(int year, Gender gender)
    {
        return (Gender == Gender.Mixed || Gender == gender) &&
               (BirthYearMin ?? 0) <= year &&
               year <= (BirthYearMax ?? int.MaxValue);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;
        if (BirthYearMin.HasValue && BirthYearMax.HasValue && BirthYearMin.Value > BirthYearMax.Value)
        {
            yield return new ValidationResult(
                ValidationStrings.AgeGroup_InvalidYearRange,
                [nameof(BirthYearMin), nameof(BirthYearMax)]);
        }
        var existed = currContext?.AgeGroups
            .FirstOrDefault(ageGroup => ageGroup.Gender == Gender &&
                                        ageGroup.BirthYearMin == BirthYearMin &&
                                        ageGroup.BirthYearMax == BirthYearMax);
        if (existed != null && currContext.Entry(this).State == EntityState.Added)
        {
            yield return new ValidationResult(ValidationStrings.AgeGroup_AlreadyExists, [nameof(AgeGroup)]);
        }
    }
}
