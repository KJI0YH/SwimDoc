using System.Collections;
using System.Globalization;
using System.Reflection;
using DataLayer.EfClasses;

namespace DataLayer.Logging;

public static class EntityLogFormatter
{
    private const int MaxStringLength = 300;
    private const int MaxDepth = 4;
    private const int MaxCollectionItems = 100;

    public static string FormatOperation(string operation, object? entity)
    {
        if (entity is null)
            return $"{operation} null";

        try
        {
            return $"{operation} {entity.GetType().Name}={Serialize(entity)}";
        }
        catch (Exception ex)
        {
            return $"{operation} {entity.GetType().Name}={{<serialization failed: {ex.Message}>}}";
        }
    }

    public static string FormatIdList(IEnumerable<int> ids) => string.Join(", ", ids);

    private static string Serialize(object? value, int depth = 0)
    {
        if (value is null)
            return "null";
        if (depth > MaxDepth)
            return "...";

        return value switch
        {
            string text => $"\"{Escape(Truncate(text))}\"",
            char ch => $"'{ch}'",
            bool boolean => boolean ? "true" : "false",
            DateOnly date => date.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            Enum enumValue => enumValue.ToString() ?? "null",
            _ when value.GetType().IsPrimitive => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null",
            _ when value is IEnumerable enumerable and not string => SerializeEnumerable(enumerable, depth),
            _ => SerializeObject(value, depth)
        };
    }

    private static string SerializeEnumerable(IEnumerable enumerable, int depth)
    {
        var items = new List<string>();
        foreach (var item in enumerable)
        {
            if (items.Count >= MaxCollectionItems)
            {
                items.Add("...");
                break;
            }
            items.Add(Serialize(item, depth + 1));
        }
        return $"[{string.Join(", ", items)}]";
    }

    private static string SerializeObject(object value, int depth)
    {
        var type = value.GetType();
        var parts = new List<string>();
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;
            if (ShouldSkipProperty(property, value))
                continue;

            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            parts.Add($"{property.Name}={Serialize(propertyValue, depth + 1)}");
        }
        return $"{{{string.Join(", ", parts)}}}";
    }

    private static bool ShouldSkipProperty(PropertyInfo property, object parent)
    {
        var propertyType = property.PropertyType;

        if (propertyType == typeof(string) || propertyType.IsPrimitive || propertyType.IsEnum ||
            propertyType == typeof(DateOnly) || propertyType == typeof(TimeOnly) ||
            propertyType == typeof(DateTime) || propertyType == typeof(DateTimeOffset))
            return false;

        var underlying = Nullable.GetUnderlyingType(propertyType);
        if (underlying is not null &&
            (underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(DateOnly) ||
             underlying == typeof(TimeOnly) || underlying == typeof(DateTime) ||
             underlying == typeof(DateTimeOffset)))
            return false;

        if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
        {
            var elementType = GetEnumerableElementType(propertyType);
            return elementType?.Name is not (nameof(HeatPosition) or nameof(RelayPosition));
        }

        if (parent is Entry && propertyType == typeof(Relay))
            return false;
        if (parent is HeatPosition && propertyType == typeof(Entry))
            return false;

        if (propertyType.Namespace == typeof(Entry).Namespace && propertyType.IsClass)
            return true;

        return propertyType.IsClass;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
        if (!type.IsGenericType)
            return null;
        return type.GetGenericArguments()[0];
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .ReplaceLineEndings("\\n");

    private static string Truncate(string value) =>
        value.Length <= MaxStringLength ? value : $"{value[..MaxStringLength]}...";
}
