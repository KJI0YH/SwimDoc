using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfClasses;

public class Heat : IValidatableObject
{
    public int Id { get; set; }
    public int Number { get; set; }
    public int Order { get; set; }
    public HeatStatus Status { get; set; }
    public TimeOnly? DayTime { get; set; }

    public string DisplayDayTime => DayTime?.ToString("HH:mm") ?? "Не назначено";

    public string DisplayNumberWithTime =>
        DayTime.HasValue ? $"{Number} ({DisplayDayTime})" : Number.ToString();

    public int SwimEventId { get; set; }
    public SwimEvent SwimEvent { get; set; }

    public ICollection<HeatPosition> Positions { get; set; }

    [NotMapped]
    public int TotalHeatsInEvent { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currContext = validationContext.GetService(typeof(DbContext));
        //TODO implement validate method
        throw new NotImplementedException();
    }
}