namespace DataLayer.EfClasses;

public class RelayPosition
{
    public int Order { get; set; }
    public int? EntryTime { get; set; }

    public int RelayId { get; set; }
    public Relay Relay { get; set; }

    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; }
}