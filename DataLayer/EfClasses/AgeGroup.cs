namespace DataLayer.EfClasses;

public class AgeGroup
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Gender Gender { get; set; }
    public int? YearMin { get; set; }
    public int? YearMax { get; set; }
    public ICollection<SwimEvent> Events { get; set; }
}