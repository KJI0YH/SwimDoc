using UI.Resources;

namespace UI.Helpers;

public sealed class EnumOption<T>(T value) where T : struct, Enum
{
    public T Value { get; } = value;

    public string Display => Strings.GetEnumDisplay(value);

    public override string ToString() => Display;
}
