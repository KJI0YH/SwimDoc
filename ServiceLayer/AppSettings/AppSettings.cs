namespace ServiceLayer.AppSettings;

public sealed class AppSettings
{
    public string? Language { get; set; }
    public int? FontSize { get; set; }
    public Dictionary<string, int>? PageSizes { get; set; }
    public string? EntryImportHighlightScoringMode { get; set; }
}
