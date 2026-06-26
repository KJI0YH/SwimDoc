using DataLayer.EfClasses;
using DataLayer.Display;

namespace UI.Models.Fixation;

public sealed class ResultEntryView(int place, Entry entry)
{
    public int Place { get; } = place;
    public int? RankingPlace => EntryTimeDisplay.IsDisqualifiedResult(Entry) ? null : Place;
    public Entry Entry { get; } = entry;
    public string PlaceDisplay => EntryTimeDisplay.FormatResultPlace(Entry, Place);
    public string ParticipantName => EntityDisplayFormatter.FormatEntryParticipantName(Entry);
    public string ParticipantYearOfBirth => Entry.Athlete?.YearOfBirth.ToString()
        ?? EntityDisplayFormatter.FormatEntryParticipantBirthYear(Entry);
    public string ParticipantCategory => EntityDisplayFormatter.FormatAthleteCategory(Entry.Athlete);
    public string ClubName => EntityDisplayFormatter.FormatEntryParticipantClubName(Entry);
    public string ResultText => EntryTimeDisplay.FormatResultTime(Entry);
    public int? Points => Entry.Points;
}
