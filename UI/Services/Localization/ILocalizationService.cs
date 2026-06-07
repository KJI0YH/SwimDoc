using System.Globalization;

namespace UI.Services.Localization;

public interface ILocalizationService
{
    AppLanguage CurrentLanguage { get; }
    event Action<CultureInfo> CultureChanged;
    void SetLanguage(AppLanguage language);
}
