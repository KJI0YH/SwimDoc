namespace DataLayer.EfClasses;

public class HeatPosition
{
    public int HeatId { get; set; }
    public int EntryId { get; set; }
    public int Lane { get; set; }


    public Entry Entry { get; set; }
    public Heat Heat { get; set; }
}