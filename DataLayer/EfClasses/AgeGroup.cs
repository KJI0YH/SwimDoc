using System.ComponentModel.DataAnnotations;
using DataLayer.EfCore;
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext)) as EfCoreContext;
        if (BirthYearMin.HasValue && BirthYearMax.HasValue && BirthYearMin.Value > BirthYearMax.Value)
        {
            yield return new ValidationResult("Invalid year range", [nameof(BirthYearMin), nameof(BirthYearMax)]);
        }

        var existed = currContext.AgeGroups.FirstOrDefault(ageGroup => ageGroup.Gender == Gender && ageGroup.BirthYearMin == BirthYearMin && ageGroup.BirthYearMax == BirthYearMax);
        if (existed != null)
        {
            yield return new ValidationResult("Age group already exists", [nameof(AgeGroup)]);
        }
    }
}