using System.ComponentModel;

namespace BizLogic.Helpers;

public static class EnumHelper
{
    public static bool TryGetEnumByDescription<T>(string desc, out T enumValue) where T : struct
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException($"{typeof(T)} is not an enum");
        foreach (T item in Enum.GetValues(typeof(T)))
        {
            var field = typeof(T).GetField(item.ToString());
            if (field == null) continue;
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (attributes == null || attributes.Length == 0 || attributes[0].Description != desc) continue;
            enumValue = item;
            return true;
        }

        enumValue = default(T);
        return false;
    }
    
    public static bool TryGetEnumByDescriptionContains<T>(string desc, out T enumValue) where T : struct
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException($"{typeof(T)} is not an enum");
        foreach (T item in Enum.GetValues(typeof(T)))
        {
            var field = typeof(T).GetField(item.ToString());
            if (field == null) continue;
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (attributes == null || attributes.Length == 0 || !attributes[0].Description.Contains(desc)) continue;
            enumValue = item;
            return true;
        }

        enumValue = default(T);
        return false;
    }
}