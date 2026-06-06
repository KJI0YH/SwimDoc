using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfCore;

public static class SwimDocDbFunctions
{
    [DbFunction("swimdoc_contains", IsBuiltIn = false)]
    public static bool ContainsIgnoreCase(string? source, string? value)
        => throw new InvalidOperationException("For EF Core translation only.");
}
