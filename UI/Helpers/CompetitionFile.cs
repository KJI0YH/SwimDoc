using System.IO;

namespace UI.Helpers;

public static class CompetitionFile
{
    public const string Extension = ".swimdb";

    public static string? ResolveStartupPath(IReadOnlyList<string> args)
    {
        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
                continue;

            var trimmed = arg.Trim().Trim('"');
            if (trimmed.StartsWith('-') || trimmed.StartsWith('/'))
                continue;

            return Path.GetFullPath(trimmed);
        }

        return null;
    }
}
