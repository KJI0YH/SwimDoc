using System.IO;
using System.Text.Json;
using UI.Resources;

namespace UI.Services;

public sealed class PagingSettingsService : IPagingSettingsService
{
    public static readonly IReadOnlyList<PagingPage> NavigationOrder =
    [
        PagingPage.Events,
        PagingPage.Entries,
        PagingPage.Heats,
        PagingPage.Athletes,
        PagingPage.Clubs,
        PagingPage.AgeGroups,
        PagingPage.SwimStyles
    ];

    private const string SettingsFileName = "ui-paging-settings.json";
    private const int MinPageSize = 1;
    private const int MaxPageSize = 500;

    private static readonly IReadOnlyDictionary<PagingPage, int> Defaults =
        new Dictionary<PagingPage, int>
        {
            [PagingPage.Entries] = 30,
            [PagingPage.Events] = 30,
            [PagingPage.Athletes] = 30,
            [PagingPage.Clubs] = 30,
            [PagingPage.AgeGroups] = 30,
            [PagingPage.SwimStyles] = 30,
            [PagingPage.Heats] = 10
        };

    private readonly Dictionary<PagingPage, int> _pageSizes = new(Defaults);

    public event Action<PagingPage>? PageSizeChanged;

    public PagingSettingsService()
    {
        LoadFromDisk();
    }

    public int GetPageSize(PagingPage page) => _pageSizes[page];

    public int GetDefaultPageSize(PagingPage page) => Defaults[page];

    public int SetPageSize(PagingPage page, int pageSize)
    {
        var normalized = Normalize(pageSize);
        if (_pageSizes[page] == normalized)
            return normalized;

        _pageSizes[page] = normalized;
        SaveToDisk();
        PageSizeChanged?.Invoke(page);
        return normalized;
    }

    public static string GetPageTitle(PagingPage page) =>
        page switch
        {
            PagingPage.Entries => Strings.Nav_Entries,
            PagingPage.Events => Strings.Nav_Program,
            PagingPage.Athletes => Strings.Nav_Athletes,
            PagingPage.Clubs => Strings.Nav_Clubs,
            PagingPage.AgeGroups => Strings.Nav_AgeGroups,
            PagingPage.SwimStyles => Strings.Nav_SwimStyles,
            PagingPage.Heats => Strings.Nav_Heats,
            _ => page.ToString()
        };

    private static int Normalize(int pageSize) => Math.Clamp(pageSize, MinPageSize, MaxPageSize);

    private void LoadFromDisk()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
                return;

            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<PagingSettingsDto>(json);
            if (dto?.PageSizes is null)
                return;

            foreach (var (key, value) in dto.PageSizes)
            {
                if (!Enum.TryParse<PagingPage>(key, ignoreCase: true, out var page))
                    continue;

                _pageSizes[page] = Normalize(value);
            }
        }
        catch
        {

        }
    }

    private void SaveToDisk()
    {
        try
        {
            var dto = new PagingSettingsDto
            {
                PageSizes = _pageSizes.ToDictionary(
                    pair => pair.Key.ToString(),
                    pair => pair.Value)
            };

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetSettingsPath(), json);
        }
        catch
        {

        }
    }

    private static string GetSettingsPath() =>
        Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName);
}
