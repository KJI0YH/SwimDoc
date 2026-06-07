using System.Globalization;
using System.IO;

namespace UI.Services.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private const string SettingsFileName = "ui-language.txt";
    private AppLanguage _currentLanguage;
    public event Action<CultureInfo>? CultureChanged;
    public AppLanguage CurrentLanguage => _currentLanguage;
    public LocalizationService()
    {
        _currentLanguage = LoadLanguageFromDisk() ?? AppLanguage.Russian;
        ApplyCulture(_currentLanguage);
    }

    public void SetLanguage(AppLanguage language)
    {
        if (_currentLanguage == language)
            return;
        _currentLanguage = language;
        SaveLanguageToDisk(language);
        ApplyCulture(language);
    }

    private void ApplyCulture(AppLanguage language)
    {
        var culture = language == AppLanguage.Russian
            ? CultureInfo.GetCultureInfo("ru-RU")
            : CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureChanged?.Invoke(culture);
    }

    private static AppLanguage? LoadLanguageFromDisk()
    {
        try
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName);
            if (!File.Exists(path))
                return null;
            var text = File.ReadAllText(path).Trim();
            return Enum.TryParse<AppLanguage>(text, ignoreCase: true, out var value) ? value : null;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveLanguageToDisk(AppLanguage language)
    {
        try
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName);
            File.WriteAllText(path, language.ToString());
        }
        catch
        {
        }
    }
}
