namespace BizLogic.EntryDocumentReader;

public static class AthleteNameImportParser
{
    public static (string lastName, string firstName) SplitFullName(string fullName, bool firstNameFirst = false)
    {
        var parts = fullName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (firstNameFirst)
        {
            return parts.Length switch
            {
                0 => (string.Empty, string.Empty),
                1 => (string.Empty, parts[0]),
                _ => (parts[^1], string.Join(' ', parts.Take(parts.Length - 1)))
            };
        }

        return parts.Length switch
        {
            0 => (string.Empty, string.Empty),
            1 => (parts[0], string.Empty),
            _ => (parts[0], string.Join(' ', parts.Skip(1)))
        };
    }
}
