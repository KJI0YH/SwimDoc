using System.ComponentModel.DataAnnotations;

namespace DataLayer.EfClasses;

public class Club
{
    public int Id { get; set; }
    [MinLength(1)]
    public string Name { get; set; }
    public string? ShortName { get; set; }
    public ClubType Type { get; set; }

    public ICollection<Athlete> Athletes { get; set; }
    public ICollection<Relay> Relays { get; set; }
}