using System.ComponentModel;
using System.Reflection;

namespace DataLayer;

public static class EnumDisplay
{
    public static string GetDescription<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var type = typeof(TEnum);
        var name = Enum.GetName(type, value);
        if (name == null)
            return value.ToString() ?? string.Empty;

        var field = type.GetField(name);
        if (field == null)
            return name;

        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return !string.IsNullOrEmpty(attr?.Description) ? attr.Description : name;
    }

    public static string GetDescription(Enum value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name == null)
            return value.ToString() ?? string.Empty;

        var field = type.GetField(name);
        if (field == null)
            return name;

        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        return !string.IsNullOrEmpty(attr?.Description) ? attr.Description : name;
    }
}
