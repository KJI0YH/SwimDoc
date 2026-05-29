using System.Globalization;

namespace UI.Services;

public enum AppLanguage
{
    English,
    Russian
}

public interface ILocalizationService
{
    AppLanguage CurrentLanguage { get; }
    event Action<CultureInfo> CultureChanged;

    void SetLanguage(AppLanguage language);
}

