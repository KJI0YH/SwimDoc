using System.Globalization;
using ServiceLayer.AppSettings;

namespace UI.Services.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private readonly IAppSettingsStore _settingsStore;
    private AppLanguage _currentLanguage;

    public event Action<CultureInfo>? CultureChanged;
    public AppLanguage CurrentLanguage => _currentLanguage;

    public LocalizationService(IAppSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _currentLanguage = LoadLanguageFromStore() ?? AppLanguage.Russian;
        ApplyCulture(_currentLanguage);
    }

    public void SetLanguage(AppLanguage language)
    {
        if (_currentLanguage == language)
            return;
        _currentLanguage = language;
        SaveLanguageToStore(language);
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

    private AppLanguage? LoadLanguageFromStore()
    {
        var languageText = _settingsStore.Get().Language;
        if (string.IsNullOrWhiteSpace(languageText))
            return null;
        return Enum.TryParse<AppLanguage>(languageText, ignoreCase: true, out var value) ? value : null;
    }

    private void SaveLanguageToStore(AppLanguage language)
    {
        _settingsStore.Update(settings => settings.Language = language.ToString());
    }
}
