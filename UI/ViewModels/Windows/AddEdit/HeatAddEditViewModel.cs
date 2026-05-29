using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.Helpers;
using UI.Resources;
using UI.Services;
using UI.ViewModels;
using UI.Views.Controls.SearchableComboBox;

namespace UI.ViewModels.Windows.AddEdit;

public partial class HeatAddEditViewModel(
    int? id,
    IHeatService heatService,
    IEntryService entryService,
    IEventService eventService)
    : ViewModelBase, IWindowResult, IAddEditContextAware
{
    private readonly int? _id = id;
    private readonly bool _isAdd = id is null or 0;
    private int? _contextEventId;
    private Heat _entity = new() { Positions = [] };

    [ObservableProperty] private ObservableCollection<string> _validationErrors = new();
    [ObservableProperty] private ObservableCollection<HeatPositionEditorRow> _positionRows = new();
    private List<SearchableItem> _allEntryItems = [];
    private bool _isRefreshingRowEntries;
    [ObservableProperty] private string _swimEventDisplayName = string.Empty;
    [ObservableProperty] private int _laneMin = 1;
    [ObservableProperty] private int _laneMax = 1;

    public int MaxPositionCount => LaneMax - LaneMin + 1;
    public int MinHeatNumber => 1;

    public bool HasErrors => ValidationErrors.Count > 0;
    public bool WasSaved { get; private set; }
    public bool IsReadOnly => false;
    public bool CanEditFields => true;
    public string WindowTitle => _isAdd ? Strings.WindowTitle_CreateHeat : Strings.WindowTitle_EditHeat;
    object? IWindowResult.Result => _entity;

    public event EventHandler? CloseRequested;

    public IReadOnlyList<string> HourOptions { get; } = CreateTimePartOptions(24);
    public IReadOnlyList<string> MinuteOptions { get; } = CreateTimePartOptions(60);
    public Array HeatStatusValues => Enum.GetValues<HeatStatus>();

    public int Number
    {
        get => _entity.Number;
        set
        {
            _entity.Number = value < MinHeatNumber ? MinHeatNumber : value;
            OnPropertyChanged();
        }
    }

    public HeatStatus Status
    {
        get => _entity.Status;
        set
        {
            _entity.Status = value;
            OnPropertyChanged();
        }
    }

    public string HourText
    {
        get => _entity.DayTime?.Hour.ToString("00") ?? string.Empty;
        set
        {
            if (!TryParseTimePart(value, 23, out var hour))
                return;

            UpdateDayTime(hour, _entity.DayTime?.Minute);
        }
    }

    public string MinuteText
    {
        get => _entity.DayTime?.Minute.ToString("00") ?? string.Empty;
        set
        {
            if (!TryParseTimePart(value, 59, out var minute))
                return;

            UpdateDayTime(_entity.DayTime?.Hour, minute);
        }
    }

    public void ApplyContext(AddEditContext context)
    {
        _contextEventId = context.EventId;
    }

    public async Task InitializeAsync()
    {
        if (_isAdd)
        {
            if (!_contextEventId.HasValue)
                throw new InvalidOperationException(Strings.Heat_Validation_EventRequired);

            _entity = new Heat
            {
                SwimEventId = _contextEventId.Value,
                Status = HeatStatus.NOT_STARTED,
                Positions = []
            };
            _entity.Number = await heatService.GetNextHeatNumberAsync(_contextEventId.Value);
            SetPositionRows(new ObservableCollection<HeatPositionEditorRow>());
        }
        else
        {
            var loadedHeat = await heatService.Query()
                .Include(heat => heat.SwimEvent)
                .ThenInclude(swimEvent => swimEvent.AgeGroup)
                .Include(heat => heat.SwimEvent)
                .ThenInclude(swimEvent => swimEvent.SwimStyle)
                .Include(heat => heat.Positions)
                .ThenInclude(position => position.Entry)
                .ThenInclude(entry => entry.Athlete!)
                .ThenInclude(athlete => athlete.Club)
                .Include(heat => heat.Positions)
                .ThenInclude(position => position.Entry)
                .ThenInclude(entry => entry.Relay!)
                .ThenInclude(relay => relay.Club)
                .FirstOrDefaultAsync(heat => heat.Id == _id);

            _entity = loadedHeat ?? new Heat { Positions = [] };
            _contextEventId = _entity.SwimEventId;
            SetPositionRows(new ObservableCollection<HeatPositionEditorRow>(
                _entity.Positions
                    .OrderBy(position => position.Lane)
                    .Select(position => CreateRowFromPosition(position))));
        }

        await LoadSwimEventInfoAsync();
        await LoadAvailableEntriesAsync();
        OnPropertyChanged(nameof(Number));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(HourText));
        OnPropertyChanged(nameof(MinuteText));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(CanEditFields));
        OnPropertyChanged(nameof(WindowTitle));
        AddPositionRowCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddPositionRow))]
    private void AddPositionRow()
    {
        PositionRows.Add(new HeatPositionEditorRow
        {
            Lane = SuggestNextLane(),
            RemoveCommand = new RelayCommand<HeatPositionEditorRow>(RemovePositionRow)
        });
    }

    [RelayCommand(CanExecute = nameof(CanModifyPositions))]
    private void RemovePositionRow(HeatPositionEditorRow? row)
    {
        if (row is null)
            return;

        PositionRows.Remove(row);
    }

    private bool CanModifyPositions() => !IsReadOnly;

    private bool CanAddPositionRow() => CanModifyPositions() && PositionRows.Count < MaxPositionCount;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ValidationErrors.Clear();

        if (Number < MinHeatNumber)
        {
            ValidationErrors.Add(Strings.Heat_Validation_MinHeatNumber);
            return;
        }

        var filledRows = PositionRows
            .Where(row => row.SelectedEntry?.Value is Entry)
            .ToList();
        if (filledRows.Count > MaxPositionCount)
        {
            ValidationErrors.Add(
                string.Format(Strings.Heat_Validation_MaxPositionsFormat, MaxPositionCount, LaneMin, LaneMax));
            return;
        }

        _entity.Positions = filledRows
            .Select(row => new HeatPosition
            {
                HeatId = _entity.Id,
                Lane = row.Lane,
                EntryId = ((Entry)row.SelectedEntry!.Value!).Id
            })
            .ToList();

        var (savedHeat, errors) = await heatService.SaveHeatWithPositionsAsync(_entity, _isAdd);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ValidationErrors.Add(error.ErrorMessage ?? Strings.Validation_ErrorFallback);
            return;
        }

        _entity = savedHeat ?? _entity;
        WasSaved = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSave() => !IsReadOnly && _contextEventId.HasValue;

    private async Task LoadSwimEventInfoAsync()
    {
        if (!_contextEventId.HasValue)
        {
            SwimEventDisplayName = string.Empty;
            return;
        }

        var swimEvent = await eventService.Query()
            .Include(se => se.AgeGroup)
            .Include(se => se.SwimStyle)
            .FirstOrDefaultAsync(se => se.Id == _contextEventId.Value);

        SwimEventDisplayName = EntityDisplayFormatter.FormatSwimEvent(swimEvent);
        if (swimEvent is not null)
        {
            LaneMin = swimEvent.LaneMin;
            LaneMax = swimEvent.LaneMax;
        }

        OnPropertyChanged(nameof(MaxPositionCount));
        AddPositionRowCommand.NotifyCanExecuteChanged();
    }

    private void SetPositionRows(ObservableCollection<HeatPositionEditorRow> rows)
    {
        if (PositionRows is not null)
        {
            PositionRows.CollectionChanged -= OnPositionRowsChanged;
            foreach (var row in PositionRows)
                UnsubscribeFromRow(row);
        }

        PositionRows = rows;
        foreach (var row in PositionRows)
            SubscribeToRow(row);
        PositionRows.CollectionChanged += OnPositionRowsChanged;
    }

    private void OnPositionRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (HeatPositionEditorRow row in e.NewItems)
                SubscribeToRow(row);
        }

        if (e.OldItems is not null)
        {
            foreach (HeatPositionEditorRow row in e.OldItems)
                UnsubscribeFromRow(row);
        }

        RefreshRowAvailableEntries();
        AddPositionRowCommand.NotifyCanExecuteChanged();
    }

    private void SubscribeToRow(HeatPositionEditorRow row) =>
        row.SelectedEntryChanged += OnRowSelectedEntryChanged;

    private void UnsubscribeFromRow(HeatPositionEditorRow row) =>
        row.SelectedEntryChanged -= OnRowSelectedEntryChanged;

    private void OnRowSelectedEntryChanged(object? sender, EventArgs e)
    {
        if (_isRefreshingRowEntries)
            return;

        RefreshRowAvailableEntries();
    }

    partial void OnLaneMinChanged(int value) => OnPropertyChanged(nameof(MaxPositionCount));

    partial void OnLaneMaxChanged(int value) => OnPropertyChanged(nameof(MaxPositionCount));

    private async Task LoadAvailableEntriesAsync()
    {
        if (!_contextEventId.HasValue)
        {
            _allEntryItems = [];
            RefreshRowAvailableEntries();
            return;
        }

        var assignedEntryIds = await heatService.Query()
            .Where(heat => heat.SwimEventId == _contextEventId.Value && (!_isAdd ? heat.Id != _entity.Id : true))
            .SelectMany(heat => heat.Positions.Select(position => position.EntryId))
            .ToListAsync();

        var currentHeatEntryIds = PositionRows
            .Where(row => row.EntryId > 0)
            .Select(row => row.EntryId)
            .Concat(_entity.Positions.Select(position => position.EntryId))
            .ToHashSet();

        var entries = await entryService.Query()
            .Where(entry => entry.SwimEventId == _contextEventId.Value)
            .Include(entry => entry.Athlete)
            .ThenInclude(athlete => athlete!.Club)
            .Include(entry => entry.Relay)
            .ThenInclude(relay => relay!.Club)
            .OrderBy(entry => entry.EntryTime == null)
            .ThenBy(entry => entry.EntryTime)
            .ToListAsync();

        _allEntryItems = entries
            .Where(entry => !assignedEntryIds.Contains(entry.Id) || currentHeatEntryIds.Contains(entry.Id))
            .Select(entry => new SearchableItem
            {
                Value = entry,
                DisplayText = FormatEntryDisplay(entry)
            })
            .ToList();

        RefreshRowAvailableEntries();
    }

    private void RefreshRowAvailableEntries()
    {
        _isRefreshingRowEntries = true;
        foreach (var row in PositionRows)
            row.SuppressSelectionChanges = true;

        try
        {
            foreach (var row in PositionRows)
            {
                var entryId = row.EntryId;
                var usedInOtherRows = PositionRows
                    .Where(other => other != row && other.EntryId > 0)
                    .Select(other => other.EntryId)
                    .ToHashSet();

                row.AvailableEntries.Clear();
                foreach (var item in _allEntryItems)
                {
                    if (item.Value is not Entry entry)
                        continue;

                    if (!usedInOtherRows.Contains(entry.Id) || entryId == entry.Id)
                        row.AvailableEntries.Add(item);
                }

                if (entryId <= 0)
                {
                    row.SelectedEntry = null;
                    continue;
                }

                var match = row.AvailableEntries.FirstOrDefault(item =>
                    item.Value is Entry entry && entry.Id == entryId);
                if (!ReferenceEquals(row.SelectedEntry, match))
                    row.SelectedEntry = match;
            }
        }
        finally
        {
            foreach (var row in PositionRows)
                row.SuppressSelectionChanges = false;

            _isRefreshingRowEntries = false;
        }
    }

    private HeatPositionEditorRow CreateRowFromPosition(HeatPosition position)
    {
        var row = new HeatPositionEditorRow
        {
            Lane = position.Lane,
            EntryId = position.EntryId,
            SelectedEntry = new SearchableItem
            {
                Value = position.Entry,
                DisplayText = FormatEntryDisplay(position.Entry)
            },
            RemoveCommand = new RelayCommand<HeatPositionEditorRow>(RemovePositionRow)
        };
        return row;
    }

    private int SuggestNextLane()
    {
        if (!_contextEventId.HasValue)
            return 1;

        var swimEvent = eventService.Query().FirstOrDefault(se => se.Id == _contextEventId.Value);
        if (swimEvent is null)
            return 1;

        for (var lane = swimEvent.LaneMin; lane <= swimEvent.LaneMax; lane++)
        {
            if (PositionRows.All(row => row.Lane != lane))
                return lane;
        }

        return swimEvent.LaneMin;
    }

    private void UpdateDayTime(int? hour, int? minute)
    {
        if (!hour.HasValue && !minute.HasValue)
            _entity.DayTime = null;
        else
            _entity.DayTime = new TimeOnly(hour ?? 0, minute ?? 0);

        OnPropertyChanged(nameof(HourText));
        OnPropertyChanged(nameof(MinuteText));
    }

    private static bool TryParseTimePart(string? text, int max, out int? value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return true;
        }

        if (int.TryParse(text.Trim(), out var parsed) && parsed >= 0 && parsed <= max)
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static List<string> CreateTimePartOptions(int count) =>
        Enumerable.Range(0, count).Select(value => value.ToString("00")).ToList();

    private static string FormatEntryDisplay(Entry entry) =>
        $"{entry.DisplayParticipantName} ({entry.DisplayEntryTime})";
}

public partial class HeatPositionEditorRow : ObservableObject
{
    public ObservableCollection<SearchableItem> AvailableEntries { get; } = new();

    internal bool SuppressSelectionChanges { get; set; }

    [ObservableProperty] private int _lane;
    [ObservableProperty] private int _entryId;
    [ObservableProperty] private SearchableItem? _selectedEntry;
    [ObservableProperty] private IRelayCommand<HeatPositionEditorRow>? _removeCommand;

    public event EventHandler? SelectedEntryChanged;

    partial void OnSelectedEntryChanged(SearchableItem? value)
    {
        if (!SuppressSelectionChanges)
        {
            if (value?.Value is Entry entry)
                EntryId = entry.Id;
            else if (value is null)
                EntryId = 0;

            SelectedEntryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
