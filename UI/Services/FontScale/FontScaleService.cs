using System.Windows;
using ServiceLayer.AppSettings;

namespace UI.Services.FontScale;

public sealed class FontScaleService : IFontScaleService
{
    public const int DefaultFontSize = 16;
    public const int MinFontSize = 8;
    public const int MaxFontSize = 24;

    private static readonly string[] FontSizeResourceKeys =
    [
        "SwimDocFontSizeCaption",
        "SwimDocFontSizeBody",
        "SwimDocFontSizeSubtitle",
        "SwimDocFontSizeTitle",
        "SwimDocFontSizeDisplay",
        "ControlContentThemeFontSize",
        "ContentControlFontSize"
    ];

    private readonly IAppSettingsStore _settingsStore;

    public FontScaleService(IAppSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        CurrentFontSize = Normalize(LoadFontSizeFromStore() ?? DefaultFontSize);
    }

    public void ApplyCurrent() => Apply(CurrentFontSize);

    public int CurrentFontSize { get; private set; }
    public bool CanDecrease => CurrentFontSize > MinFontSize;
    public bool CanIncrease => CurrentFontSize < MaxFontSize;
    public event Action<int>? FontSizeChanged;

    public int SetFontSize(int fontSize) => SetFontSizeInternal(fontSize);

    private int SetFontSizeInternal(int fontSize)
    {
        fontSize = Normalize(fontSize);
        if (CurrentFontSize == fontSize)
            return fontSize;
        CurrentFontSize = fontSize;
        SaveFontSizeToStore(fontSize);
        Apply(fontSize);
        FontSizeChanged?.Invoke(fontSize);
        return fontSize;
    }

    private static int Normalize(int fontSize) =>
        Math.Clamp(fontSize, MinFontSize, MaxFontSize);

    private static void Apply(int fontSize)
    {
        if (Application.Current?.Resources is not ResourceDictionary resources)
            return;
        var size = (double)fontSize;
        foreach (var key in FontSizeResourceKeys)
            resources[key] = size;
    }

    private int? LoadFontSizeFromStore() => _settingsStore.Get().FontSize;

    private void SaveFontSizeToStore(int fontSize) =>
        _settingsStore.Update(settings => settings.FontSize = fontSize);
}
