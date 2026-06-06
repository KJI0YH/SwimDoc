using System.Globalization;

namespace UI.Services;

public interface ILocalizationService
{
    AppLanguage CurrentLanguage { get; }
    event Action<CultureInfo> CultureChanged;

    void SetLanguage(AppLanguage language);
}
