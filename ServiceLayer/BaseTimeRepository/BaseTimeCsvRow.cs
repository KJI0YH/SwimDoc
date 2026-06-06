namespace ServiceLayer.BaseTimeRepository;

internal sealed class BaseTimeCsvRow
{
    public string course { get; set; } = string.Empty;
    public int meters { get; set; }
    public string stroke { get; set; } = string.Empty;
    public string sex { get; set; } = string.Empty;
    public int relaycount { get; set; }
    public int basetime { get; set; }
}
