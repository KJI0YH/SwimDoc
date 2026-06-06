using DataLayer.EfClasses;
using UI.Helpers;

namespace UI.Models.Fixation;

public sealed class ResultEntryView(int place, Entry entry)
{
    public int Place { get; } = place;
    public Entry Entry { get; } = entry;

    public string ParticipantName => EntityDisplayFormatter.FormatEntryParticipantName(Entry);
    public string ParticipantYearOfBirth => Entry.Athlete?.YearOfBirth.ToString()
        ?? EntityDisplayFormatter.FormatEntryParticipantBirthYear(Entry);
    public string ClubName => EntityDisplayFormatter.FormatEntryParticipantClubName(Entry);

    public string ResultText => EntityDisplayFormatter.FormatFinishTime(Entry);
    public int? Points => Entry.Points;
}
