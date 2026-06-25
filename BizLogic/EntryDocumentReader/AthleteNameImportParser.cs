namespace BizLogic.EntryDocumentReader;

public static class AthleteNameImportParser
{
    public static (string lastName, string firstName) SplitFullName(string fullName)
    {
        var parts = fullName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => (string.Empty, string.Empty),
            1 => (parts[0], string.Empty),
            _ => (parts[0], string.Join(' ', parts.Skip(1)))
        };
    }
}
