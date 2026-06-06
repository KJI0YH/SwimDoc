using DataLayer.EfClasses;
using UI.Helpers;

namespace UI.Models.Fixation;

public sealed class ParticipantResultEntryView(ResultEntryView result, int? athleteId = null)
{
    public int Place => result.Place;
    public Entry Entry => result.Entry;

    public string EventName => EntityDisplayFormatter.FormatEntrySwimName(Entry);
    public string ParticipantName => result.ParticipantName;
    public string ParticipantYearOfBirth => athleteId is int id
        ? GetAthleteBirthYear(Entry, id)
        : result.ParticipantYearOfBirth;
    public string ClubName => result.ClubName;
    public string ResultText => result.ResultText;
    public int? Points => result.Points;

    private static string GetAthleteBirthYear(Entry entry, int athleteId)
    {
        if (entry.AthleteId == athleteId)
            return entry.Athlete?.YearOfBirth.ToString() ?? string.Empty;

        var position = entry.Relay?.Positions.FirstOrDefault(p => p.AthleteId == athleteId);
        return position?.Athlete.YearOfBirth.ToString() ?? string.Empty;
    }
}
