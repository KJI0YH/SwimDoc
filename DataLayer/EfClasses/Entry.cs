namespace DataLayer.EfClasses;

public class Entry
{
    public int Id { get; set; }
    public int? EntryTime { get; set; }
    public bool Scoring { get; set; }
    public EntryStatus Status { get; set; }
    public string? Comment { get; set; }
    public int? FinishTime { get; set; }
    public int? Points { get; set; }

    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; }

    public int RelayId { get; set; }
    public Relay Relay { get; set; }

    public int SwimEventId { get; set; }
    public SwimEvent SwimEvent { get; set; }

    public int? HeatPositionId { get; set; }
    public HeatPosition? HeatPosition { get; set; }
}