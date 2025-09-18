using System.ComponentModel.DataAnnotations;

namespace DataLayer.EfClasses;

public class Athlete
{
    public int Id { get; set; }
    [MinLength(1)]
    public string FirstName { get; set; }
    [MinLength(1)]
    public string LastName { get; set; }
    public Gender Gender { get; set; }
    public int YearOfBirth { get; set; }
    public Category Category { get; set; }

    public int? ClubId { get; set; }
    public Club? Club { get; set; }
    public ICollection<Entry> Entries { get; set; }
    public ICollection<RelayPosition> RelayPositions { get; set; }
}