namespace ServiceLayer.RankTimeRepository;

internal sealed class RankTimeCsvRow
{
    public string course { get; set; } = string.Empty;
    public int meters { get; set; }
    public string stroke { get; set; } = string.Empty;
    public string sex { get; set; } = string.Empty;
    public int relaycount { get; set; }
    public int msmk { get; set; }
    public int ms { get; set; }
    public int kms { get; set; }
    public int i { get; set; }
    public int ii { get; set; }
    public int iii { get; set; }
    public int i_yun { get; set; }
    public int ii_yun { get; set; }
}
