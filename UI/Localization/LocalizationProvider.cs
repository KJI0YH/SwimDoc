using System.ComponentModel;
using System.Globalization;
using UI.Resources;

namespace UI.Localization;

public sealed class LocalizationProvider : INotifyPropertyChanged
{
    public static LocalizationProvider Instance { get; } = new();

    private CultureInfo _culture = CultureInfo.CurrentUICulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationProvider()
    {
    }

    public string this[string key]
    {
        get
        {
            var value = Strings.ResourceManager.GetString(key, _culture);
            return string.IsNullOrEmpty(value) ? $"[[{key}]]" : value;
        }
    }

    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            if (Equals(_culture, value)) return;
            _culture = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }
}
