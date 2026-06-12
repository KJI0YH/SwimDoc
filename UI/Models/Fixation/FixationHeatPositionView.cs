using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer;
using DataLayer.Display;
using DataLayer.EfClasses;
using ServiceLayer.PointScoreProvider;

namespace UI.Models.Fixation;

public sealed partial class FixationHeatPositionView : ObservableObject
{
    private readonly HeatPosition _position;
    private readonly SwimEvent _swimEvent;
    private readonly IPointScoreProvider _pointScoreProvider;
    private readonly Action _onChanged;
    private string _finishTimeText;
    public FixationHeatPositionView(
        HeatPosition position,
        SwimEvent swimEvent,
        Action onChanged,
        IPointScoreProvider pointScoreProvider)
    {
        _position = position;
        _swimEvent = swimEvent;
        _onChanged = onChanged;
        _pointScoreProvider = pointScoreProvider;
        if (Entry.Status < EntryStatus.FINISH)
            Entry.Status = EntryStatus.FINISH;
        _finishTimeText = SwimTimeInput.Format(Entry.FinishTime);
        CalculatePoints();
    }

    public Entry Entry => _position.Entry;
    public int EntryId => Entry.Id;
    public int Lane => _position.Lane;
    public string DisplayLane => SwimEventLaneNames.GetLaneDisplay(_swimEvent, Lane);
    public string ParticipantName => EntityDisplayFormatter.FormatEntryParticipantName(Entry);
    public string YearOfBirth => EntityDisplayFormatter.FormatEntryParticipantBirthYear(Entry);
    public string Club => EntityDisplayFormatter.FormatEntryParticipantClubName(Entry);
    public string EntryTimeDisplay => EntityDisplayFormatter.FormatEntryTime(Entry);
    public string FinishTimeDisplay => EntityDisplayFormatter.FormatFinishTime(Entry);
    public int? Points => Entry.Points;
    public IReadOnlyList<EntryStatus> StatusOptions { get; } =
    [
        EntryStatus.FINISH,
        EntryStatus.DSQ,
        EntryStatus.DNS,
        EntryStatus.DNF
    ];
    public EntryStatus SelectedStatus
    {
        get => Entry.Status;
        set
        {
            if (Entry.Status == value)
                return;
            Entry.Status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FinishTimeDisplay));
            CalculatePoints();
            _onChanged();
        }
    }

    public string FinishTimeText
    {
        get => _finishTimeText;
        set
        {
            var update = SwimTimeInput.ApplyText(value);
            if (_finishTimeText != update.Text)
            {
                _finishTimeText = update.Text;
                OnPropertyChanged();
            }
            if (Entry.FinishTime != update.Hundredths)
            {
                Entry.FinishTime = update.Hundredths;
                OnPropertyChanged(nameof(FinishTimeDisplay));
                CalculatePoints();
                _onChanged();
            }
        }
    }

    public string Comment
    {
        get => Entry.Comment ?? string.Empty;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? null : value;
            if (Entry.Comment == next)
                return;
            Entry.Comment = next;
            OnPropertyChanged();
            _onChanged();
        }
    }

    public bool IsCompleteForApproval() =>
        Entry.Status switch
        {
            EntryStatus.FINISH => Entry.FinishTime.HasValue,
            EntryStatus.DNS or EntryStatus.DNF or EntryStatus.DSQ => true,
            _ => false
        };

    private void CalculatePoints()
    {
        var points = 0;
        if (Entry.Status == EntryStatus.FINISH)
        {
            var swimStyle = Entry.SwimStyle ?? _swimEvent.SwimStyle;
            points = _pointScoreProvider.CalculatePoints(
                _swimEvent.Course,
                swimStyle.Distance,
                swimStyle.Stroke,
                swimStyle.RelayCount,
                _swimEvent.AgeGroup.Gender,
                Entry.FinishTime);
        }
        Entry.Points = points;
        OnPropertyChanged(nameof(Points));
    }
}
