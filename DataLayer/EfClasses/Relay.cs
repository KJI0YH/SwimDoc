namespace DataLayer.EfClasses;

public class Relay
{
    public int Id { get; set; }
    public int? Number { get; set; }

    public int ClubId { get; set; }
    public Club Club { get; set; }

    public Entry Entry { get; set; }
    public ICollection<RelayPosition> Positions { get; set; }
}