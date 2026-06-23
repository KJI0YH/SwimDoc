using System.IO;
using System.Text.Json;

namespace ServiceLayer.AppSettings;

public sealed class AppSettingsStore : IAppSettingsStore
{
    private const string SettingsFileName = "swimdoc-settings.json";

    private readonly object _lock = new();
    private AppSettings _settings;

    public AppSettingsStore()
    {
        _settings = Load();
    }

    public AppSettings Get()
    {
        lock (_lock)
            return Clone(_settings);
    }

    public void Update(Action<AppSettings> update)
    {
        lock (_lock)
        {
            update(_settings);
            Save(_settings);
        }
    }

    private static AppSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
                return new AppSettings();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    private static void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetSettingsPath(), json);
        }
        catch
        {
        }
    }

    private static AppSettings Clone(AppSettings settings) =>
        new()
        {
            Language = settings.Language,
            PageSizes = settings.PageSizes is null
                ? null
                : new Dictionary<string, int>(settings.PageSizes),
            EntryImportHighlightScoringMode = settings.EntryImportHighlightScoringMode
        };

    private static string GetSettingsPath() =>
        Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName);
}
