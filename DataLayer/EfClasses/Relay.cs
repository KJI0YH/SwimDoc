namespace DataLayer.EfClasses;

public class Relay
{
    public int Id { get; set; }
    public int? Number { get; set; }

    public int ClubId { get; set; }
    public Club Club { get; set; }

    public Entry Entry { get; set; }
    public ICollection<RelayPosition> Positions { get; set; }

    public string DisplayName => $"{Club.Name} {(Number.HasValue ? Number : string.Empty)}";

    public string DisplayNameWithAthletes =>
        $"{DisplayName} ({string.Join(", ", Positions.OrderBy(p => p.Order).Select(p => p.Athlete.DisplayName))})";
}