using System.Windows.Data;
using System.Windows.Markup;

namespace UI.Localization;

[MarkupExtensionReturnType(typeof(BindingExpression))]
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Key))
            return string.Empty;

        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationProvider.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
