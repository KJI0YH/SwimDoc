using DataLayer.EfClasses;
using DataLayer.Display;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.RankTimeProvider;

namespace UI.Models.Fixation;

public sealed class ResultEntryView(int place, Entry entry)
{
    private readonly IAchievedRankProvider _achievedRankProvider =
        App.Current.Services.GetRequiredService<IAchievedRankProvider>();

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
    public string RankDisplay => Entry.SwimEvent is null
        ? string.Empty
        : _achievedRankProvider.Format(Entry.SwimEvent, Entry);
    public int? Points => Entry.Points;
}
