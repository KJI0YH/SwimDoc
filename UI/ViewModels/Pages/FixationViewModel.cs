using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using UI.Helpers;
using UI.Resources;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.Views.Controls.SearchableComboBox;

namespace UI.ViewModels.Pages;

public partial class FixationViewModel(
    IEventService eventService,
    IHeatService heatService,
    IPointScoreProvider pointScoreProvider,
    INavigationService navigationService)
    : DataViewModel<SwimEvent, int?>(eventService)
{
    public event Action<int>? EventResultsChanged;

    [ObservableProperty] private SwimEvent? _selectedSwimEvent;
    [ObservableProperty] private ObservableCollection<SearchableItem> _swimEventOptions = new();

    [ObservableProperty] private Heat? _selectedHeat;

    [ObservableProperty] private ObservableCollection<Heat> _eventHeats = new();

    [ObservableProperty] private ObservableCollection<FixationHeatPositionView> _fixationHeatPositionViews = new();

    [ObservableProperty] private FixationHeatPositionView? _selectedFixationPosition;

    [ObservableProperty] private bool _canApprove;

    [ObservableProperty] private bool _canUnapprove;

    public bool CanEditHeat => SelectedHeat?.Status != HeatStatus.OFFICIAL;

    public string SelectedHeatHeader
    {
        get
        {
            if (SelectedHeat is null || SelectedSwimEvent is null) return string.Empty;

            var heatsInEvent = EventHeats.Count;
            var heatsTotal = heatService.GetTotalHeats();
            return string.Format(
                Strings.Get("Fixation_SelectedHeatHeader_NoEvent_Format"),
                SelectedHeat.Number,
                heatsInEvent,
                SelectedHeat.Order,
                heatsTotal,
                SelectedHeat.DisplayDayTime);
        }
    }

    public string SelectedHeatStatus => SelectedHeat?.Status.ToString() ?? string.Empty;

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query
            .OrderBy(se => se.Order)
            .Include(e => e.AgeGroup)
            .Include(e => e.SwimStyle);
    }

    protected override void OnItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        if (items.Count == 0)
        {
            SelectedSwimEvent = null;
            SwimEventOptions = [];
            EventHeats = [];
            SelectedHeat = null;
            FixationHeatPositionViews = [];
            return;
        }

        SelectedSwimEvent ??= items.OrderBy(e => e.Order).FirstOrDefault();

        SwimEventOptions = new ObservableCollection<SearchableItem>(
            items.Select(e => new SearchableItem
            {
                Value = e,
                DisplayText = EntityDisplayFormatter.FormatSwimEvent(e)
            }));
    }

    partial void OnSelectedSwimEventChanged(SwimEvent? value)
    {
        _ = LoadEventHeatsAsync();
        OnPropertyChanged(nameof(SelectedHeatHeader));
        OnPropertyChanged(nameof(SelectedHeatStatus));
        OnPropertyChanged(nameof(CanEditHeat));
    }

    partial void OnSelectedHeatChanged(Heat? value)
    {
        LoadHeatPositions();
        RefreshButtons();
        OnPropertyChanged(nameof(CanEditHeat));
        OnPropertyChanged(nameof(SelectedHeatHeader));
        OnPropertyChanged(nameof(SelectedHeatStatus));
    }

    private async Task LoadEventHeatsAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId)
        {
            EventHeats = [];
            SelectedHeat = null;
            FixationHeatPositionViews = [];
            return;
        }

        IsLoading = true;
        try
        {
            var heats = await heatService.GetHeatsByEventIdAsync(eventId);
            EventHeats = new ObservableCollection<Heat>(heats);
            SelectedHeat = EventHeats.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadHeatPositions()
    {
        if (SelectedHeat?.Positions is null || SelectedHeat.Positions.Count == 0)
        {
            FixationHeatPositionViews = [];
            RefreshButtons();
            return;
        }

        if (SelectedSwimEvent is null)
        {
            FixationHeatPositionViews = [];
            RefreshButtons();
            return;
        }

        var positions = SelectedHeat.Positions.OrderBy(p => p.Lane).ToList();
        FixationHeatPositionViews = new ObservableCollection<FixationHeatPositionView>(positions
            .Select(p => new FixationHeatPositionView(p, SelectedSwimEvent, OnRowChanged, pointScoreProvider)));
        RefreshButtons();
    }

    private void OnRowChanged()
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        CanUnapprove = SelectedHeat?.Status == HeatStatus.OFFICIAL;
        CanApprove = SelectedHeat is not null
                     && CanEditHeat
                     && FixationHeatPositionViews.Count > 0
                     && FixationHeatPositionViews.All(v => v.IsCompleteForApproval());

        OnPropertyChanged(nameof(CanEditHeat));
        OnPropertyChanged(nameof(SelectedHeatHeader));
        OnPropertyChanged(nameof(SelectedHeatStatus));
    }

    [RelayCommand]
    private async Task ApproveHeat()
    {
        if (SelectedHeat is null) return;
        if (!CanApprove) return;

        var keepHeatId = SelectedHeat.Id;
        try
        {
            await heatService.ApproveHeatAsync(SelectedHeat);
            await LoadEventHeatsAsync();
            SelectedHeat = EventHeats.FirstOrDefault(h => h.Id == keepHeatId) ?? SelectedHeat;
            if (SelectedSwimEvent?.Id is int eventId)
                EventResultsChanged?.Invoke(eventId);
        }
        catch
        {
            // ignored
        }
    }

    [RelayCommand]
    private async Task UnapproveHeat()
    {
        if (SelectedHeat?.Id is not int heatId)
            return;

        try
        {
            await heatService.UnapproveHeatAsync(heatId);
            await LoadEventHeatsAsync();
            if (SelectedSwimEvent?.Id is int eventId)
                EventResultsChanged?.Invoke(eventId);
        }
        catch
        {
            // ignored
        }
    }

    [RelayCommand]
    private void GoToNextEvent()
    {
        if (Items.Count == 0) return;
        var idx = SelectedSwimEvent is null ? -1 : Items.IndexOf(SelectedSwimEvent);
        var nextIdx = (idx + 1) % Items.Count;
        SelectedSwimEvent = Items[nextIdx];
    }

    [RelayCommand]
    private void GoToPrevEvent()
    {
        if (Items.Count == 0) return;
        var idx = SelectedSwimEvent is null ? 1 : Items.IndexOf(SelectedSwimEvent);
        var prevIdx = idx - 1 + (idx - 1 < 0 ? Items.Count : 0);
        SelectedSwimEvent = Items[prevIdx];
    }

    [RelayCommand]
    private void GoToNextHeat()
    {
        if (EventHeats.Count == 0) return;
        var idx = SelectedHeat is null ? -1 : EventHeats.IndexOf(SelectedHeat);
        var nextIdx = (idx + 1);
        if (nextIdx >= EventHeats.Count)
            GoToNextEvent();
        else
            SelectedHeat = EventHeats[nextIdx];
    }

    [RelayCommand]
    private void GoToPrevHeat()
    {
        if (EventHeats.Count == 0) return;

        var idx = SelectedHeat is null ? 0 : EventHeats.IndexOf(SelectedHeat);
        var prevIdx = idx - 1;
        if (prevIdx < 0)
            GoToPrevEvent();
        else
            SelectedHeat = EventHeats[prevIdx];
    }

    partial void OnSelectedFixationPositionChanged(FixationHeatPositionView? value) =>
        OpenAthleteDetailsCommand.NotifyCanExecuteChanged();

    private bool CanOpenAthleteDetails() =>
        EntryAthleteNavigationHelper.TryGetAthleteId(SelectedFixationPosition?.Entry, out _);

    [RelayCommand(CanExecute = nameof(CanOpenAthleteDetails))]
    private void OpenAthleteDetails()
    {
        if (!EntryAthleteNavigationHelper.TryGetAthleteId(SelectedFixationPosition?.Entry, out var athleteId))
            return;

        navigationService.NavigateTo<AthleteDetailsViewModel>(athleteId);
    }
}

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

        _finishTimeText = FormatTime(Entry.FinishTime);
        CalculatePoints();
    }

    public Entry Entry => _position.Entry;

    public int EntryId => Entry.Id;

    public int Lane => _position.Lane;

    public string DisplayLane => SwimEventLaneNames.GetLaneDisplay(_swimEvent, Lane);

    public string ParticipantName => Entry.DisplayParticipantName;

    public string YearOfBirth => Entry.Athlete?.YearOfBirth.ToString() ?? string.Empty;

    public string Club => Entry.DisplayParticipantClubName;

    public string EntryTimeDisplay => Entry.DisplayEntryTime;

    public string FinishTimeDisplay => Entry.DisplayFinishTime;

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
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            var parsed = ParseTimeFromDigits(digits);
            var formatted = FormatTime(parsed);

            if (_finishTimeText != formatted)
            {
                _finishTimeText = formatted;
                OnPropertyChanged();
            }

            if (Entry.FinishTime != parsed)
            {
                Entry.FinishTime = parsed;
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
            points = _pointScoreProvider.CalculatePoints(
                _swimEvent.Course,
                _swimEvent.SwimStyle.Distance,
                _swimEvent.SwimStyle.Stroke,
                _swimEvent.SwimStyle.RelayCount,
                _swimEvent.AgeGroup.Gender,
                Entry.FinishTime);
        Entry.Points = points;
        OnPropertyChanged(nameof(Points));
    }

    private static string FormatTime(int? value)
    {
        if (!value.HasValue)
            return string.Empty;

        var totalHundredths = value.Value;
        if (totalHundredths < 0)
            totalHundredths = 0;

        var minutes = totalHundredths / 6000;
        var seconds = totalHundredths % 6000 / 100;
        var hundredths = totalHundredths % 100;

        return minutes > 0
            ? $"{minutes}:{seconds:D2}.{hundredths:D2}"
            : $"{seconds}.{hundredths:D2}";
    }

    private static int? ParseTimeFromDigits(string digits)
    {
        if (string.IsNullOrWhiteSpace(digits))
            return null;

        var normalized = digits.TrimStart('0');
        if (normalized.Length == 0)
            normalized = "0";
        if (normalized.Length > 9)
            normalized = normalized[^9..];

        var padded = normalized.PadLeft(4, '0');
        var hundredthsPart = padded[^2..];
        var secondsPart = padded[^4..^2];
        var minutesPart = padded.Length > 4 ? padded[..^4] : "0";

        if (!int.TryParse(minutesPart, out var minutes))
            minutes = 0;
        if (!int.TryParse(secondsPart, out var seconds))
            seconds = 0;
        if (!int.TryParse(hundredthsPart, out var hundredths))
            hundredths = 0;

        return minutes * 6000 + seconds * 100 + hundredths;
    }
}