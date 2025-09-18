namespace DataLayer.EfClasses;

public class SwimStyle
{
    public int Id { get; set; }
    public int RelayCount { get; set; }
    public Stroke Stroke { get; set; }
    public int Distance { get; set; }
    public ICollection<SwimEvent> Events { get; set; }
}