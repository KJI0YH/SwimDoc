using DataLayer.EfClasses;
using BizLogic.Resources;

namespace BizLogic.ReportGenerator.Concrete.Excel;

internal static class ReportEntryDisplayHelper
{
    public static string GetParticipantName(Entry entry)
    {
        if (entry.Athlete is not null)
            return FormatAthleteName(entry.Athlete);

        if (entry.Relay is not null)
            return GetRelayParticipantName(entry.Relay);

        return ReportExcelStrings.Value_NoneParen;
    }

    public static string? GetBirthYear(Entry entry)
    {
        if (entry.Athlete is not null)
            return entry.Athlete.YearOfBirth.ToString();

        if (entry.Relay is not null)
            return GetRelayBirthYears(entry.Relay);

        return null;
    }

    public static string GetTeamName(Entry entry)
    {
        if (entry.Relay is not null)
            return entry.Relay.Club?.Name ?? ReportExcelStrings.Value_PersonalParen;

        return entry.Athlete?.Club?.Name ?? ReportExcelStrings.Value_PersonalParen;
    }

    private static string GetRelayParticipantName(Relay relay)
    {
        var clubName = relay.Club?.Name ?? ReportExcelStrings.Value_PersonalParen;
        var numberPart = relay.Number.HasValue ? $" {relay.Number}" : string.Empty;
        var athleteNames = relay.Positions?
            .OrderBy(position => position.Order)
            .Select(position => position.Athlete is null ? null : FormatAthleteName(position.Athlete))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList() ?? [];

        if (athleteNames.Count == 0)
            return $"{clubName}{numberPart}".Trim();

        return $"{clubName}{numberPart} ({string.Join(", ", athleteNames)})".Trim();
    }

    private static string? GetRelayBirthYears(Relay relay)
    {
        var years = relay.Positions?
            .OrderBy(position => position.Order)
            .Select(position => position.Athlete?.YearOfBirth)
            .Where(year => year.HasValue)
            .Select(year => year!.Value.ToString())
            .ToList() ?? [];

        return years.Count == 0 ? null : string.Join(", ", years);
    }

    private static string FormatAthleteName(Athlete athlete) =>
        $"{athlete.FirstName} {athlete.LastName}";
}
