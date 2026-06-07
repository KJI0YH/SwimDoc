using DataLayer;
using DataLayer.Display;
using DataLayer.EfClasses;
using UI.Resources;

namespace UI.Models.Fixation;

public sealed class HeatPositionView(
    HeatPosition heatPosition,
    SwimEvent? swimEvent,
    int heatNumber,
    int heatsInEvent,
    int heatOrder,
    int heatsTotal,
    HeatStatus heatStatus,
    string heatDayTime)
{
    private HeatPosition HeatPosition { get; set; } = heatPosition;
    public Entry Entry => HeatPosition.Entry;
    public int HeatId => HeatPosition.HeatId;
    public int EntryId => HeatPosition.EntryId;
    public HeatStatus HeatStatus { get; } = heatStatus;
    public string HeatGroupHeader => string.Format(
        Strings.Heats_GroupHeader_Format,
        heatNumber,
        heatsInEvent,
        heatOrder,
        heatsTotal,
        heatDayTime,
        heatStatus);
    public int Lane => HeatPosition.Lane;
    public string DisplayLane => swimEvent is not null
        ? SwimEventLaneNames.GetLaneDisplay(swimEvent, HeatPosition.Lane)
        : HeatPosition.Lane.ToString();
    public string Participant => EntityDisplayFormatter.FormatEntryParticipantName(HeatPosition.Entry);
    public int? YearOfBirth => HeatPosition.Entry.Athlete?.YearOfBirth;
    public string Club => EntityDisplayFormatter.FormatEntryParticipantClubName(HeatPosition.Entry);
    public string EntryTime => EntityDisplayFormatter.FormatEntryTime(HeatPosition.Entry);
    public string FinishTime => EntityDisplayFormatter.FormatFinishTime(HeatPosition.Entry);
}
