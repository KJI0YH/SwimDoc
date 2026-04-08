using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;

namespace UI.ViewModels.Pages;

public partial class HeatsResultsViewModel(IEventService eventService, IHeatService heatService)
    : HeatsViewModel(eventService, heatService)
{
    private int? _pendingSelectHeatId;

    [ObservableProperty] private ObservableCollection<HeatManualResultRowViewModel> _manualResultRows = new();
    private readonly IHeatService _heatService = heatService;

    public IReadOnlyList<EntryStatus> ManualResultStatusOptions { get; } =
    [
        EntryStatus.FINISH,
        EntryStatus.DNS,
        EntryStatus.DNF,
        EntryStatus.DSQ
    ];

    public bool IsOfficial => SelectedHeat?.Status == HeatStatus.OFFICIAL;

    public bool CanEdit => !IsOfficial && SelectedHeat is not null;

    public bool CanApprove =>
        !IsOfficial &&
        SelectedHeat is not null &&
        ManualResultRows.Count > 0 &&
        ManualResultRows.All(r => r.IsResultProvided);

    public bool CanUnapprove => IsOfficial && SelectedHeat is not null;

    public string SelectedHeatHeader
    {
        get
        {
            if (SelectedSwimEvent is null || SelectedHeat is null)
                return string.Empty;

            var totalInEvent = SelectedHeat.TotalHeatsInEvent > 0
                ? SelectedHeat.TotalHeatsInEvent
                : SelectedSwimEvent.Heats.Count;

            return
                $"{SelectedSwimEvent.DisplayName} — Заплыв {SelectedHeat.Number} из {totalInEvent} ({SelectedHeat.Order} из {TotalHeats})";
        }
    }

    public string SelectedHeatStatusText => SelectedHeat?.Status.ToString() ?? string.Empty;

    protected override bool AfterItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        if (!_pendingSelectHeatId.HasValue || Items.Count == 0)
            return false;

        var ordered = Items
            .OrderBy(se => se.Order)
            .SelectMany(se => se.Heats.OrderBy(h => h.Number).Select(h => (eventRef: se, heatRef: h)))
            .ToList();

        var match = ordered.FirstOrDefault(x => x.heatRef.Id == _pendingSelectHeatId.Value);
        _pendingSelectHeatId = null;

        if (match.heatRef is null)
            return false;

        SuppressSwimEventHeatAutoPick = true;
        try
        {
            SelectedSwimEvent = match.eventRef;
            SelectedHeat = match.heatRef;
        }
        finally
        {
            SuppressSwimEventHeatAutoPick = false;
        }

        return true;
    }

    protected override void OnSelectedHeatChangedCore(Heat? value)
    {
        RebuildManualRows();
        OnPropertyChanged(nameof(IsOfficial));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanUnapprove));
        OnPropertyChanged(nameof(SelectedHeatHeader));
        OnPropertyChanged(nameof(SelectedHeatStatusText));
    }

    private void RebuildManualRows()
    {
        if (SelectedHeat?.Positions is null)
        {
            ManualResultRows = new ObservableCollection<HeatManualResultRowViewModel>();
            return;
        }

        var rows = SelectedHeat.Positions
            .OrderBy(p => p.Lane)
            .Select(p => new HeatManualResultRowViewModel(
                lane: p.Lane,
                entryId: p.EntryId,
                participantName: p.Entry?.Athlete?.DisplayName ?? string.Empty,
                participantYearOfBirth: p.Entry?.Athlete?.YearOfBirth.ToString() ?? string.Empty,
                clubName: p.Entry?.Athlete?.DisplayClubName ?? string.Empty,
                entryTimeDisplay: p.Entry?.DisplayEntryTime ?? string.Empty,
                finishTime: p.Entry?.FinishTime,
                status: p.Entry?.Status ?? EntryStatus.HEAT,
                reason: p.Entry?.Comment,
                statusOptions: ManualResultStatusOptions))
            .ToList();

        ManualResultRows = new ObservableCollection<HeatManualResultRowViewModel>(rows);
        foreach (var row in ManualResultRows)
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(HeatManualResultRowViewModel.IsResultProvided))
                    OnPropertyChanged(nameof(CanApprove));
            };
    }

    [RelayCommand]
    private async Task SaveResultsAsync()
    {
        if (!CanEdit || SelectedHeat is null)
            return;

        _pendingSelectHeatId = SelectedHeat.Id;
        await _heatService.UpdateHeatResultsAsync(
            SelectedHeat.Id,
            ManualResultRows.Select(r => r.ToIn()).ToList());

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task ApproveHeatAsync()
    {
        if (!CanApprove || SelectedHeat is null)
            return;

        _pendingSelectHeatId = SelectedHeat.Id;
        await _heatService.UpdateHeatResultsAsync(
            SelectedHeat.Id,
            ManualResultRows.Select(r => r.ToIn()).ToList());
        await _heatService.ApproveHeatAsync(SelectedHeat.Id);

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task UnapproveHeatAsync()
    {
        if (!CanUnapprove || SelectedHeat is null)
            return;

        _pendingSelectHeatId = SelectedHeat.Id;
        await _heatService.UnapproveHeatAsync(SelectedHeat.Id);
        await LoadDataAsync();
    }
}

public sealed partial class HeatManualResultRowViewModel : ObservableObject
{
    public HeatManualResultRowViewModel(
        int lane,
        int entryId,
        string participantName,
        string participantYearOfBirth,
        string clubName,
        string entryTimeDisplay,
        int? finishTime,
        EntryStatus status,
        string? reason,
        IReadOnlyList<EntryStatus> statusOptions)
    {
        Lane = lane;
        EntryId = entryId;
        ParticipantName = participantName;
        ParticipantYearOfBirth = participantYearOfBirth;
        ClubName = clubName;
        EntryTimeDisplay = entryTimeDisplay;
        StatusOptions = statusOptions;

        SelectedStatus = statusOptions.Contains(status) ? status : EntryStatus.FINISH;
        Reason = reason ?? string.Empty;

        FinishTime = finishTime;
        _finishTimeText = TimeDigitsHelper.FormatDisplay(finishTime);
    }

    public int Lane { get; }
    public int EntryId { get; }
    public string ParticipantName { get; }
    public string ParticipantYearOfBirth { get; }
    public string ClubName { get; }
    public string EntryTimeDisplay { get; }

    [ObservableProperty] private int? _finishTime;

    private string _finishTimeText = string.Empty;
    public string FinishTimeText
    {
        get => _finishTimeText;
        set
        {
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            var parsed = TimeDigitsHelper.ParseFromDigits(digits);
            var formatted = TimeDigitsHelper.FormatDisplay(parsed);

            if (_finishTimeText != formatted)
            {
                _finishTimeText = formatted;
                OnPropertyChanged();
            }

            if (FinishTime != parsed)
            {
                FinishTime = parsed;
                OnPropertyChanged(nameof(FinishTimeDisplay));
                OnPropertyChanged(nameof(IsResultProvided));
            }

            if (parsed.HasValue && SelectedStatus != EntryStatus.FINISH)
            {
                SelectedStatus = EntryStatus.FINISH;
                OnPropertyChanged(nameof(IsResultProvided));
            }
        }
    }

    public string FinishTimeDisplay => TimeDigitsHelper.FormatDisplay(FinishTime);

    public IReadOnlyList<EntryStatus> StatusOptions { get; }

    [ObservableProperty] private EntryStatus _selectedStatus;

    partial void OnSelectedStatusChanged(EntryStatus value)
    {
        if (value is EntryStatus.DNS or EntryStatus.DNF or EntryStatus.DSQ)
        {
            if (FinishTime is not null)
            {
                FinishTime = null;
                _finishTimeText = string.Empty;
                OnPropertyChanged(nameof(FinishTimeText));
                OnPropertyChanged(nameof(FinishTimeDisplay));
            }
        }

        OnPropertyChanged(nameof(IsResultProvided));
    }

    [ObservableProperty] private string _reason = string.Empty;

    public bool IsResultProvided =>
        SelectedStatus switch
        {
            EntryStatus.FINISH => FinishTime.HasValue,
            EntryStatus.DNS or EntryStatus.DNF or EntryStatus.DSQ => true,
            _ => false
        };

    public HeatLaneResultIn ToIn()
    {
        return new HeatLaneResultIn
        {
            EntryId = EntryId,
            FinishTime = FinishTime,
            Status = SelectedStatus,
            Comment = Reason
        };
    }
}

internal static class TimeDigitsHelper
{
    public static int? ParseFromDigits(string digits)
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

    public static string FormatDisplay(int? value)
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
}

