namespace DataLayer.EfClasses;

public class SwimEvent
{
    public int Id { get; set; }
    public int? Order { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public EventRound Round { get; set; }
    public int? RoundParticipantsCount { get; set; }
    public int LaneMin { get; set; }
    public int LaneMax { get; set; }

    public int AgeGroupId { get; set; }
    public AgeGroup AgeGroup { get; set; }

    public int SwimStyleId { get; set; }
    public SwimStyle SwimStyle { get; set; }

    public ICollection<Heat> Heats { get; set; }
    public ICollection<Entry> Entries { get; set; }

    public int? PreviousSwimEventId { get; set; }
    public SwimEvent? PreviousSwimEvent { get; set; }
    public SwimEvent? NextSwimEvent { get; set; }
}