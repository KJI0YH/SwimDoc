using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using UI.Resources;
using UI.Models;
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Dialogs.AddEdit;

public partial class EntryViewModel(
    int? id,
    IEntryService entryService,
    IAthleteService athleteService,
    IClubService clubService,
    IEventService eventService)
    : AddEditViewModel<Entry, int?>(id, entryService), INavigationContextAware
{
    private bool _initialized;
    private bool _suppressRelaySync;
    private bool _suppressIndividualFilterSync;
    private List<Athlete> _allAthletes = [];
    private List<SwimEvent> _allIndividualSwimEvents = [];
    private HashSet<(int AthleteId, int SwimEventId)> _existingIndividualEntryKeys = [];
    private bool _relayNumberManuallySet;
    private string _relayNumberText = string.Empty;
    public bool IsInitialized => _initialized;
    [ObservableProperty] private ObservableCollection<SearchableItem> _athletes = new();
    private int? _contextAthleteId;
    private int? _contextClubId;
    private int? _contextEventId;
    private int? _contextSwimStyleId;
    private string _entryTimeText = string.Empty;
    [ObservableProperty] private SearchableItem? _selectedAthlete;
    [ObservableProperty] private SearchableItem? _selectedSwimEvent;
    [ObservableProperty] private ObservableCollection<SearchableItem> _swimEvents = new();
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _isIndividualTabEnabled = true;
    [ObservableProperty] private bool _isRelayTabEnabled = true;
    [ObservableProperty] private ObservableCollection<SearchableItem> _relaySwimEvents = new();
    [ObservableProperty] private SearchableItem? _selectedRelaySwimEvent;
    [ObservableProperty] private ObservableCollection<SearchableItem> _clubs = new();
    [ObservableProperty] private SearchableItem? _selectedClub;
    [ObservableProperty] private ObservableCollection<SearchableItem> _relayAthletes = new();
    [ObservableProperty] private ObservableCollection<RelayRowViewModel> _relayLegs = new();
    public int? RelayNumber
    {
        get => Entity.Relay?.Number;
        set
        {
            EnsureRelayEntity();
            Entity.Relay!.Number = value;
            OnPropertyChanged();
            var formatted = value?.ToString() ?? string.Empty;
            if (_relayNumberText != formatted)
            {
                _relayNumberText = formatted;
                OnPropertyChanged(nameof(RelayNumberText));
            }
        }
    }

    public string RelayNumberText
    {
        get => _relayNumberText;
        set
        {
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            int? parsed = null;
            if (!string.IsNullOrWhiteSpace(digits) && int.TryParse(digits, out var n))
                parsed = n;
            var formatted = parsed?.ToString() ?? string.Empty;
            if (_relayNumberText != formatted)
            {
                _relayNumberText = formatted;
                OnPropertyChanged();
            }
            EnsureRelayEntity();
            if (Entity.Relay!.Number != parsed)
            {
                Entity.Relay.Number = parsed;
                _relayNumberManuallySet = true;
                OnPropertyChanged(nameof(RelayNumber));
            }
        }
    }

    public override string WindowTitle => IsAdd ? Strings.WindowTitle_CreateEntry : Strings.WindowTitle_EditEntry;
    public int? EntryTime
    {
        get => Entity.EntryTime;
        set
        {
            Entity.EntryTime = value;
            OnPropertyChanged();
            var formatted = SwimTimeInput.Format(value);
            if (_entryTimeText != formatted)
            {
                _entryTimeText = formatted;
                OnPropertyChanged(nameof(EntryTimeText));
            }
        }
    }

    public string EntryTimeText
    {
        get => _entryTimeText;
        set
        {
            var update = SwimTimeInput.ApplyText(value);
            if (_entryTimeText != update.Text)
            {
                _entryTimeText = update.Text;
                OnPropertyChanged();
            }
            if (Entity.EntryTime != update.Hundredths)
            {
                Entity.EntryTime = update.Hundredths;
                OnPropertyChanged(nameof(EntryTime));
            }
        }
    }

    public bool Scoring
    {
        get => Entity.Scoring;
        set
        {
            Entity.Scoring = value;
            OnPropertyChanged();
        }
    }

    public EntryStatus Status
    {
        get => Entity.Status;
        set
        {
            Entity.Status = value;
            OnPropertyChanged();
        }
    }

    public string? Comment
    {
        get => Entity.Comment;
        set
        {
            Entity.Comment = value;
            OnPropertyChanged();
        }
    }

    public int? FinishTime
    {
        get => Entity.FinishTime;
        set
        {
            Entity.FinishTime = value;
            OnPropertyChanged();
        }
    }

    public int? Points
    {
        get => Entity.Points;
        set
        {
            Entity.Points = value;
            OnPropertyChanged();
        }
    }

    public Array EntryStatusValues => Enum.GetValues<EntryStatus>();
    public void ApplyContext(NavigationContext context)
    {
        _contextAthleteId = context.AthleteId;
        _contextEventId = context.EventId;
        _contextClubId = context.ClubId;
        _contextSwimStyleId = context.SwimStyleId;
    }

    protected override async Task<Entry?> LoadEntityAsync(int? id)
    {
        return await CrudService.Query()
            .Include(entry => entry.Athlete)
            .Include(entry => entry.Relay)
            .ThenInclude(relay => relay.Club)
            .Include(entry => entry.Relay)
            .ThenInclude(relay => relay.Positions)
            .ThenInclude(pos => pos.Athlete)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(swimEvent => swimEvent!.AgeGroup)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(swimEvent => swimEvent!.SwimStyle)
            .Include(entry => entry.SwimStyle)
            .FirstOrDefaultAsync(entry => entry.Id == id);
    }

    public override async Task InitializeAsync()
    {
        if (_initialized)
            return;
        await base.InitializeAsync();
        SelectedTabIndex = 0;
        IsIndividualTabEnabled = true;
        IsRelayTabEnabled = true;
        await LoadAllIndividualEventsAsync();
        await LoadRelayEventsAsync();
        LoadClubs();
        LoadAllAthletes();
        await LoadExistingIndividualEntryKeysAsync();
        _entryTimeText = SwimTimeInput.Format(Entity.EntryTime);
        OnPropertyChanged(nameof(EntryTimeText));
        OnPropertyChanged(nameof(Scoring));
        if (IsAdd)
            ApplyAddContextDefaults();
        if (!IsAdd && Entity.Relay is not null)
        {
            SelectedTabIndex = 1;
            _suppressRelaySync = true;
            SelectedClub = Clubs.FirstOrDefault(item => item.Value is Club c && c.Id == Entity.Relay.ClubId);
            _suppressRelaySync = false;
            _suppressRelaySync = true;
            SelectedRelaySwimEvent = Entity.SwimEventId == null
                ? null
                : RelaySwimEvents.FirstOrDefault(item => item.Value is SwimEvent se && se.Id == Entity.SwimEventId);
            _suppressRelaySync = false;
            if (!IsAdd && Entity.RelayId is not null)
                await LoadRelayPositionsByRelayIdAsync(Entity.RelayId.Value);
            await LoadRelayAthletesAsync();
            await EnsureRelayAthletesContainRelayPositionAthletesAsync();
            _relayNumberText = Entity.Relay.Number?.ToString() ?? string.Empty;
            OnPropertyChanged(nameof(RelayNumberText));
            OnPropertyChanged(nameof(RelayNumber));
            BuildRelayLegsFromEntity();
            ApplyRelaySelectionsFromEntityPositions();
            RefreshRelayLegAthleteOptions();
        }
        if (IsAdd)
            ApplyAddContextUiState();
        RefreshIndividualEntryOptions();
        _initialized = true;
    }

    private void ApplyAddContextDefaults()
    {
        if (_contextAthleteId is int athleteId)
            Entity.AthleteId = athleteId;
        if (_contextEventId is int eventId)
        {
            if (!IsRelayContextEvent(eventId))
                ApplyIndividualEventToEntity(eventId);
        }
        else if (_contextSwimStyleId is int swimStyleId && !IsRelayContextSwimStyle(swimStyleId))
            ApplyIndividualSwimStyleToEntity(swimStyleId);
    }

    private void ApplyAddContextUiState()
    {
        if (_contextEventId is int eventId)
            ApplyAddContextForEvent(eventId);
        else if (_contextSwimStyleId is int swimStyleId)
            ApplyAddContextForSwimStyle(swimStyleId);
        else if (_contextAthleteId.HasValue)
        {
            SelectedTabIndex = 0;
            IsIndividualTabEnabled = true;
            IsRelayTabEnabled = true;
        }
    }

    private bool IsRelayContextEvent(int eventId) =>
        RelaySwimEvents.Any(item => item.Value is SwimEvent swimEvent && swimEvent.Id == eventId);

    private bool IsRelayContextSwimStyle(int swimStyleId) =>
        RelaySwimEvents.Any(item => item.Value is SwimEvent swimEvent && swimEvent.SwimStyleId == swimStyleId);

    private void ApplyIndividualEventToEntity(int eventId)
    {
        var swimEvent = _allIndividualSwimEvents.FirstOrDefault(e => e.Id == eventId);
        if (swimEvent is null)
            return;
        Entity.SwimEventId = swimEvent.Id;
        Entity.SwimStyleId = swimEvent.SwimStyleId;
    }

    private void ApplyIndividualSwimStyleToEntity(int swimStyleId)
    {
        var swimEvent = _allIndividualSwimEvents.FirstOrDefault(e => e.SwimStyleId == swimStyleId);
        if (swimEvent is null)
            return;
        Entity.SwimEventId = swimEvent.Id;
        Entity.SwimStyleId = swimEvent.SwimStyleId;
    }

    private void ApplyAddContextForEvent(int eventId)
    {
        var relayContextEvent = RelaySwimEvents.FirstOrDefault(item =>
            item.Value is SwimEvent se && se.Id == eventId);
        if (relayContextEvent != null)
        {
            ApplyRelayEventContext(relayContextEvent);
            return;
        }
        SelectedTabIndex = 0;
        IsRelayTabEnabled = false;
    }

    private void ApplyAddContextForSwimStyle(int swimStyleId)
    {
        var relayContextEvent = RelaySwimEvents.FirstOrDefault(item =>
            item.Value is SwimEvent se && se.SwimStyleId == swimStyleId);
        if (relayContextEvent != null)
        {
            ApplyRelayEventContext(relayContextEvent);
            return;
        }
        SelectedTabIndex = 0;
        if (_allIndividualSwimEvents.Any(e => e.SwimStyleId == swimStyleId))
            IsRelayTabEnabled = false;
    }

    private void ApplyRelayEventContext(SearchableItem relayContextEvent)
    {
        if (relayContextEvent.Value is not SwimEvent swimEvent)
            return;
        _suppressRelaySync = true;
        SelectedRelaySwimEvent = relayContextEvent;
        _suppressRelaySync = false;
        EnsureRelayEntity();
        Entity.SwimEventId = swimEvent.Id;
        Entity.SwimStyleId = swimEvent.SwimStyleId;
        Entity.AthleteId = null;
        Entity.EntryTime = null;
        BuildRelayRowsForCount(swimEvent.SwimStyle.RelayCount);
        SelectedTabIndex = 1;
        IsIndividualTabEnabled = false;
        _ = TryAssignDefaultRelayNumberAsync();
    }

    private void LoadAllAthletes()
    {
        var query = athleteService.Query();
        if (_contextAthleteId.HasValue)
            query = query.Where(a => a.Id == _contextAthleteId.Value);
        else if (_contextClubId.HasValue)
            query = query.Where(a => a.ClubId == _contextClubId.Value);
        _allAthletes = query
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ToList();
    }

    private async Task LoadAllIndividualEventsAsync()
    {
        _allIndividualSwimEvents = (await eventService.GetIndividualEventsAsync())
            .OrderBy(swimEvent => swimEvent.Order)
            .ThenBy(swimEvent => swimEvent.Date)
            .ToList();
    }

    private async Task LoadExistingIndividualEntryKeysAsync()
    {
        var entries = await entryService.Query()
            .AsNoTracking()
            .Where(entry => entry.AthleteId != null && entry.RelayId == null && entry.SwimEventId != null)
            .Select(entry => new { entry.Id, entry.AthleteId, entry.SwimEventId })
            .ToListAsync();
        _existingIndividualEntryKeys = entries
            .Where(entry => IsAdd || entry.Id != Entity.Id)
            .Select(entry => (entry.AthleteId!.Value, entry.SwimEventId!.Value))
            .ToHashSet();
    }

    private bool IndividualEntryExists(int athleteId, int swimEventId) =>
        _existingIndividualEntryKeys.Contains((athleteId, swimEventId));

    private void RefreshIndividualEntryOptions()
    {
        if (SelectedTabIndex != 0)
            return;

        var selectedAthleteId = (SelectedAthlete?.Value as Athlete)?.Id
            ?? Entity.AthleteId
            ?? _contextAthleteId;
        var selectedEventId = (SelectedSwimEvent?.Value as SwimEvent)?.Id
            ?? Entity.SwimEventId
            ?? _contextEventId;
        var selectedAthlete = selectedAthleteId is int athleteId
            ? _allAthletes.FirstOrDefault(a => a.Id == athleteId)
            : null;
        var selectedEvent = selectedEventId is int eventId
            ? _allIndividualSwimEvents.FirstOrDefault(e => e.Id == eventId)
            : null;

        var filteredAthletes = _allAthletes.AsEnumerable();
        var filteredEvents = _allIndividualSwimEvents.AsEnumerable();
        if (selectedAthlete is not null)
        {
            filteredEvents = filteredEvents.Where(swimEvent =>
                swimEvent.AgeGroup.Contains(selectedAthlete.YearOfBirth, selectedAthlete.Gender));
            filteredEvents = filteredEvents.Where(swimEvent =>
                !IndividualEntryExists(selectedAthlete.Id, swimEvent.Id));
        }
        if (selectedEvent is not null)
        {
            filteredAthletes = filteredAthletes.Where(athlete =>
                selectedEvent.AgeGroup.Contains(athlete.YearOfBirth, athlete.Gender));
            filteredAthletes = filteredAthletes.Where(athlete =>
                !IndividualEntryExists(athlete.Id, selectedEvent.Id));
        }

        _suppressIndividualFilterSync = true;
        try
        {
            SyncAthleteItems(filteredAthletes);
            SyncSwimEventItems(filteredEvents);
            SelectedAthlete = selectedAthleteId is int restoredAthleteId
                ? Athletes.FirstOrDefault(item => item.Value is Athlete athlete && athlete.Id == restoredAthleteId)
                : null;
            SelectedSwimEvent = selectedEventId is int restoredEventId
                ? SwimEvents.FirstOrDefault(item => item.Value is SwimEvent swimEvent && swimEvent.Id == restoredEventId)
                : SwimEvents.FirstOrDefault(item => item.Value == null);
            if (SelectedAthlete?.Value is Athlete athlete)
                Entity.AthleteId = athlete.Id;
            else if (selectedAthleteId is not null && SelectedAthlete is null)
                Entity.AthleteId = null;
            if (SelectedSwimEvent?.Value is SwimEvent swimEvent)
            {
                Entity.SwimEventId = swimEvent.Id;
                Entity.SwimStyleId = swimEvent.SwimStyleId;
            }
            else if (selectedEventId is not null && SelectedSwimEvent?.Value is null)
            {
                Entity.SwimEventId = null;
            }
        }
        finally
        {
            _suppressIndividualFilterSync = false;
        }
    }

    private void SyncAthleteItems(IEnumerable<Athlete> athletes)
    {
        Athletes.Clear();
        foreach (var athlete in athletes)
        {
            Athletes.Add(new SearchableItem
            {
                Value = athlete,
                DisplayText = EntityDisplayFormatter.FormatAthleteName(athlete)
            });
        }
    }

    private void SyncSwimEventItems(IEnumerable<SwimEvent> swimEvents)
    {
        SwimEvents.Clear();
        SwimEvents.Add(new SearchableItem { Value = null, DisplayText = string.Empty });
        foreach (var swimEvent in swimEvents)
        {
            SwimEvents.Add(new SearchableItem
            {
                Value = swimEvent,
                DisplayText = EntityDisplayFormatter.FormatSwimEvent(swimEvent)
            });
        }
    }

    private async Task LoadRelayEventsAsync()
    {
        RelaySwimEvents.Clear();
        var relayEvents = await eventService.GetRelayEventsAsync();
        if (_contextEventId.HasValue && relayEvents.Count != 0)
        {
            var relayEvent = relayEvents.FirstOrDefault(se => se.Id == _contextEventId.Value);
            if (relayEvent != null)
                RelaySwimEvents.Add(new SearchableItem { Value = relayEvent, DisplayText = EntityDisplayFormatter.FormatSwimEvent(relayEvent) });
        }
        else if (_contextSwimStyleId.HasValue && relayEvents.Count != 0)
        {
            var relayEvent = relayEvents.FirstOrDefault(se => se.SwimStyleId == _contextSwimStyleId.Value);
            if (relayEvent != null)
                RelaySwimEvents.Add(new SearchableItem { Value = relayEvent, DisplayText = EntityDisplayFormatter.FormatSwimEvent(relayEvent) });
        }
        else
        {
            foreach (var swimEvent in relayEvents)
                RelaySwimEvents.Add(new SearchableItem { Value = swimEvent, DisplayText = EntityDisplayFormatter.FormatSwimEvent(swimEvent) });
        }
        if (IsAdd
            && !_contextAthleteId.HasValue
            && Entity.SwimEventId == null
            && SelectedRelaySwimEvent == null
            && RelaySwimEvents.Count > 0)
        {
            if (_contextEventId.HasValue)
                SelectedRelaySwimEvent = RelaySwimEvents.FirstOrDefault(i =>
                    i.Value is SwimEvent se && se.Id == _contextEventId.Value);
            else if (_contextSwimStyleId.HasValue)
                SelectedRelaySwimEvent = RelaySwimEvents.FirstOrDefault(i =>
                    i.Value is SwimEvent se && se.SwimStyleId == _contextSwimStyleId.Value);
        }
    }

    private void LoadClubs()
    {
        var clubs = clubService.Query().ToList();
        Clubs.Clear();
        foreach (var club in clubs)
            Clubs.Add(new SearchableItem { Value = club, DisplayText = club.Name });
    }

    private async Task LoadRelayAthletesAsync()
    {
        RelayAthletes.Clear();
        if (SelectedClub?.Value is not Club club) return;
        var athletes = await athleteService.GetAthletesByClubIdAsync(club.Id);
        foreach (var athlete in athletes)
            RelayAthletes.Add(new SearchableItem { Value = athlete, DisplayText = EntityDisplayFormatter.FormatAthleteName(athlete) });
    }

    private async Task LoadRelayPositionsByRelayIdAsync(int relayId)
    {
        var entryWithRelay = await entryService.Query()
            .Where(e => e.RelayId == relayId)
            .Include(e => e.Relay)
            .ThenInclude(r => r.Positions)
            .ThenInclude(p => p.Athlete)
            .FirstOrDefaultAsync();
        if (entryWithRelay?.Relay?.Positions == null)
            return;
        EnsureRelayEntity();
        Entity.Relay!.Positions = entryWithRelay.Relay.Positions.ToList();
    }

    private async Task EnsureRelayAthletesContainRelayPositionAthletesAsync()
    {
        if (Entity.Relay?.Positions == null || Entity.Relay.Positions.Count == 0)
            return;
        var existingIds = RelayAthletes
            .Select(i => (i.Value as Athlete)?.Id)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();
        var missingIds = Entity.Relay.Positions
            .Select(p => p.AthleteId)
            .Distinct()
            .Where(id => id != 0 && !existingIds.Contains(id))
            .ToList();
        if (missingIds.Count == 0)
            return;
        var fromNav = Entity.Relay.Positions
            .Select(p => p.Athlete)
            .Where(a => a is not null && missingIds.Contains(a.Id))
            .DistinctBy(a => a!.Id)
            .Cast<Athlete>()
            .ToList();
        foreach (var athlete in fromNav)
        {
            if (existingIds.Add(athlete.Id))
                RelayAthletes.Add(new SearchableItem { Value = athlete, DisplayText = EntityDisplayFormatter.FormatAthleteName(athlete) });
        }
        var stillMissing = missingIds.Where(id => !existingIds.Contains(id)).ToList();
        if (stillMissing.Count == 0)
            return;
        var fetched = athleteService.Query()
            .Where(a => stillMissing.Contains(a.Id))
            .ToList();
        foreach (var athlete in fetched)
        {
            if (existingIds.Add(athlete.Id))
                RelayAthletes.Add(new SearchableItem { Value = athlete, DisplayText = EntityDisplayFormatter.FormatAthleteName(athlete) });
        }
    }

    partial void OnSelectedAthleteChanged(SearchableItem? value)
    {
        if (_suppressIndividualFilterSync)
            return;
        if (value?.Value is Athlete athlete)
        {
            Entity.AthleteId = athlete.Id;
            Entity.RelayId = null;
            Entity.Relay = null;
        }
        else
            Entity.AthleteId = null;
        RefreshIndividualEntryOptions();
    }

    partial void OnSelectedSwimEventChanged(SearchableItem? value)
    {
        if (_suppressIndividualFilterSync)
            return;
        if (value?.Value is SwimEvent swimEvent)
        {
            Entity.SwimEventId = swimEvent.Id;
            Entity.SwimStyleId = swimEvent.SwimStyleId;
        }
        else
            Entity.SwimEventId = null;
        RefreshIndividualEntryOptions();
    }

    partial void OnSelectedRelaySwimEventChanged(SearchableItem? value)
    {
        if (_suppressRelaySync)
            return;
        if (value?.Value is not SwimEvent swimEvent) return;
        EnsureRelayEntity();
        Entity.SwimEventId = swimEvent.Id;
        Entity.SwimStyleId = swimEvent.SwimStyleId;
        Entity.AthleteId = null;
        BuildRelayRowsForCount(swimEvent.SwimStyle.RelayCount);
        RefreshRelayLegAthleteOptions();
        _ = TryAssignDefaultRelayNumberAsync();
    }

    partial void OnSelectedClubChanged(SearchableItem? value)
    {
        if (_suppressRelaySync)
            return;
        _ = OnSelectedClubChangedAsync(value);
    }

    private async Task OnSelectedClubChangedAsync(SearchableItem? value)
    {
        if (value?.Value is not Club club) return;
        EnsureRelayEntity();
        Entity.Relay!.ClubId = club.Id;
        await LoadRelayAthletesAsync();
        await EnsureRelayAthletesContainRelayPositionAthletesAsync();
        RefreshRelayLegAthleteOptions();
        if (!_initialized && !IsAdd && (Entity.Relay?.Positions?.Count ?? 0) > 0 && RelayLegs.Count == 0)
            return;
        SyncRelayPositionsFromLegs();
        await TryAssignDefaultRelayNumberAsync();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (!IsIndividualTabEnabled && value == 0)
        {
            if (SelectedTabIndex != 1) SelectedTabIndex = 1;
            return;
        }
        if (!IsRelayTabEnabled && value == 1)
        {
            if (SelectedTabIndex != 0) SelectedTabIndex = 0;
            return;
        }
        if (value == 0)
            RefreshIndividualEntryOptions();
        if (value == 1)
        {
            EnsureRelayEntity();
            Entity.AthleteId = null;
            if (IsAdd)
                Entity.EntryTime = null;
            if (SelectedRelaySwimEvent?.Value is SwimEvent swimEvent)
            {
                Entity.SwimEventId = swimEvent.Id;
                Entity.SwimStyleId = swimEvent.SwimStyleId;
                BuildRelayRowsForCount(swimEvent.SwimStyle.RelayCount);
            }
            else if (RelayLegs.Count == 0 && Entity.SwimStyle?.IsRelay == true)
                BuildRelayRowsForCount(Entity.SwimStyle.RelayCount);
            else
                RefreshRelayLegAthleteOptions();
        }
    }

    private void EnsureRelayEntity()
    {
        if (Entity.Relay is not null) return;
        Entity.Relay = new Relay
        {
            ClubId = Entity.Athlete?.ClubId ?? _contextClubId ?? 0,
            Number = null,
            Positions = new List<RelayPosition>()
        };
        Entity.RelayId = null;
    }

    private async Task TryAssignDefaultRelayNumberAsync()
    {
        if (!IsAdd)
            return;
        if (_relayNumberManuallySet)
            return;
        if (SelectedClub?.Value is not Club club)
            return;
        if (SelectedRelaySwimEvent?.Value is not SwimEvent swimEvent)
            return;
        EnsureRelayEntity();
        if (Entity.Relay!.Number.HasValue)
            return;
        var existingCount = await entryService.Query()
            .Where(e => e.RelayId != null && e.SwimEventId == swimEvent.Id)
            .Include(e => e.Relay)
            .Where(e => e.Relay != null && e.Relay.ClubId == club.Id)
            .CountAsync();
        var next = existingCount + 1;
        RelayNumber = next <= 1 ? null : next;
    }

    private void BuildRelayLegsFromEntity()
    {
        RelayLegs.Clear();
        var positions = Entity.Relay?.Positions?.ToList() ?? [];
        var zeroBased = positions.Count > 0 && positions.Min(p => p.Order) == 0;
        var relayCount = Entity.SwimStyle?.RelayCount
                         ?? (Entity.SwimEvent?.SwimStyle?.RelayCount ?? 0);
        if (relayCount <= 0)
            relayCount = positions.Count;
        if (positions.Count > 0)
        {
            var impliedCount = zeroBased ? positions.Max(p => p.Order) + 1 : positions.Max(p => p.Order);
            relayCount = Math.Max(relayCount, impliedCount);
        }
        for (var i = 1; i <= relayCount; i++)
        {
            var pos = positions.FirstOrDefault(p => p.Order == i)
                      ?? (zeroBased ? positions.FirstOrDefault(p => p.Order == i - 1) : null);
            var vm = new RelayRowViewModel(i, RelayLegChanged)
            {
                EntryTime = pos?.EntryTime,
                SelectedAthlete = pos == null
                    ? null
                    : RelayAthletes.FirstOrDefault(a => a.Value is Athlete at && at.Id == pos.AthleteId)
            };
            RelayLegs.Add(vm);
        }
    }

    private void ApplyRelaySelectionsFromEntityPositions()
    {
        if (RelayLegs.Count == 0)
            return;
        if (Entity.Relay?.Positions == null || Entity.Relay.Positions.Count == 0)
            return;
        var positions = Entity.Relay.Positions.ToList();
        var minOrder = positions.Min(p => p.Order);
        var zeroBased = minOrder == 0;
        _suppressRelaySync = true;
        try
        {
            foreach (var leg in RelayLegs)
            {
                var pos = positions.FirstOrDefault(p => p.Order == leg.Order);
                if (pos == null && zeroBased)
                    pos = positions.FirstOrDefault(p => p.Order == leg.Order - 1);
                if (pos == null)
                    continue;
                leg.EntryTime = pos.EntryTime;
                leg.SelectedAthlete =
                    RelayAthletes.FirstOrDefault(a => a.Value is Athlete at && at.Id == pos.AthleteId);
            }
        }
        finally
        {
            _suppressRelaySync = false;
        }
        RefreshRelayLegAthleteOptions();
    }

    private void BuildRelayRowsForCount(int relayCount)
    {
        if (relayCount <= 0) return;
        var existing = RelayLegs.ToDictionary(l => l.Order, l => l);
        RelayLegs.Clear();
        for (var i = 1; i <= relayCount; i++)
        {
            if (existing.TryGetValue(i, out var old))
            {
                old.SetOnChanged(RelayLegChanged);
                RelayLegs.Add(old);
            }
            else
            {
                RelayLegs.Add(new RelayRowViewModel(i, RelayLegChanged));
            }
        }
        SyncRelayPositionsFromLegs();
        RefreshRelayLegAthleteOptions();
    }

    private void RelayLegChanged()
    {
        if (_suppressRelaySync)
            return;
        SyncRelayPositionsFromLegs();
        RefreshRelayLegAthleteOptions();
    }

    private AgeGroup? GetSelectedRelayAgeGroup()
    {
        if (SelectedRelaySwimEvent?.Value is SwimEvent selectedEvent)
            return selectedEvent.AgeGroup;
        return Entity.SwimEvent?.AgeGroup;
    }

    private IEnumerable<SearchableItem> GetEligibleRelayAthletes(AgeGroup? ageGroup) =>
        ageGroup is null
            ? []
            : RelayAthletes.Where(item =>
                item.Value is Athlete athlete &&
                ageGroup.Contains(athlete.YearOfBirth, athlete.Gender));

    private void RefreshRelayLegAthleteOptions()
    {
        var selectionByOrder = RelayLegs.ToDictionary(
            leg => leg.Order,
            leg => (leg.SelectedAthlete?.Value as Athlete)?.Id);
        var ageGroup = GetSelectedRelayAgeGroup();
        var eligible = GetEligibleRelayAthletes(ageGroup).ToList();
        var selectedAthleteIds = selectionByOrder.Values
            .Where(id => id > 0)
            .ToList();
        _suppressRelaySync = true;
        try
        {
            foreach (var leg in RelayLegs)
            {
                selectionByOrder.TryGetValue(leg.Order, out var currentAthleteId);
                var takenByOtherLegs = selectedAthleteIds
                    .Where(id => id != currentAthleteId)
                    .ToHashSet();
                var available = eligible
                    .Where(item => item.Value is Athlete athlete && !takenByOtherLegs.Contains(athlete.Id))
                    .ToList();
                if (currentAthleteId is > 0)
                {
                    var currentSelection = RelayAthletes.FirstOrDefault(item =>
                                              item.Value is Athlete athlete && athlete.Id == currentAthleteId)
                                          ?? leg.SelectedAthlete;
                    if (currentSelection is not null &&
                        available.All(item => item.Value is not Athlete athlete || athlete.Id != currentAthleteId))
                        available.Insert(0, currentSelection);
                }
                leg.SetAvailableAthletes(available);
                if (currentAthleteId is > 0)
                {
                    var restored = leg.AvailableAthletes.FirstOrDefault(item =>
                        item.Value is Athlete athlete && athlete.Id == currentAthleteId);
                    leg.SetSelectedAthleteSilently(restored);
                }
                else if (leg.SelectedAthlete is not null)
                    leg.SetSelectedAthleteSilently(null);
            }
        }
        finally
        {
            _suppressRelaySync = false;
        }
    }

    private void SyncRelayPositionsFromLegs()
    {
        EnsureRelayEntity();
        var relay = Entity.Relay!;
        relay.Positions ??= new List<RelayPosition>();
        relay.Positions.Clear();
        foreach (var leg in RelayLegs.OrderBy(l => l.Order))
        {
            if (leg.SelectedAthlete?.Value is not Athlete athlete)
                continue;
            relay.Positions.Add(new RelayPosition
            {
                Order = leg.Order,
                AthleteId = athlete.Id,
                EntryTime = leg.EntryTime
            });
        }
    }

    [RelayCommand]
    private void CreateAthlete()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var dialog = factory.CreateAndShowAndReturn<AthleteAddEditWindow>();
        if (dialog is { DialogResult: true, DataContext: IWindowResult { Result: Athlete newAthlete } })
        {
            LoadAllAthletes();
            RefreshIndividualEntryOptions();
            SelectedAthlete = Athletes.FirstOrDefault(item => item.Value is Athlete a && a.Id == newAthlete.Id);
        }
    }

    [RelayCommand]
    private async Task CreateRelayLegAthleteAsync(RelayRowViewModel? leg)
    {
        if (leg == null)
            return;
        if (SelectedClub?.Value is not Club club)
            return;
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var context = new NavigationContext { ClubId = club.Id };
        var dialog = factory.CreateAndShowAndReturn<AthleteAddEditWindow>(null, context);
        if (dialog is { DialogResult: true, DataContext: IWindowResult { Result: Athlete newAthlete } })
        {
            var athleteIdByOrder = RelayLegs.ToDictionary(
                l => l.Order,
                l => (l.SelectedAthlete?.Value as Athlete)?.Id);
            await LoadRelayAthletesAsync();
            _suppressRelaySync = true;
            try
            {
                foreach (var row in RelayLegs)
                {
                    if (!athleteIdByOrder.TryGetValue(row.Order, out var athleteId) || athleteId is null or 0)
                        continue;
                    row.SelectedAthlete = RelayAthletes.FirstOrDefault(item =>
                        item.Value is Athlete a && a.Id == athleteId.Value);
                }
                leg.SelectedAthlete = RelayAthletes.FirstOrDefault(item =>
                    item.Value is Athlete a && a.Id == newAthlete.Id);
            }
            finally
            {
                _suppressRelaySync = false;
            }
            SyncRelayPositionsFromLegs();
            RefreshRelayLegAthleteOptions();
        }
    }

    [RelayCommand]
    private async Task CreateSwimEvent()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var dialog = factory.CreateAndShowAndReturn<EventAddEditWindow>();
        if (dialog is { DialogResult: true, DataContext: IWindowResult { Result: SwimEvent newSwimEvent } })
        {
            await LoadAllIndividualEventsAsync();
            RefreshIndividualEntryOptions();
            SelectedSwimEvent =
                SwimEvents.FirstOrDefault(item => item.Value is SwimEvent se && se.Id == newSwimEvent.Id);
        }
    }

    [RelayCommand]
    private void CreateClub()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var dialog = factory.CreateAndShowAndReturn<ClubAddEditWindow>();
        if (dialog is { DialogResult: true, DataContext: IWindowResult { Result: Club newClub } })
        {
            LoadClubs();
            SelectedClub = Clubs.FirstOrDefault(item => item.Value is Club c && c.Id == newClub.Id);
        }
    }

    [RelayCommand]
    private async Task CreateRelaySwimEvent()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var dialog = factory.CreateAndShowAndReturn<EventAddEditWindow>();
        if (dialog is { DialogResult: true, DataContext: IWindowResult { Result: SwimEvent newSwimEvent } })
        {
            await LoadRelayEventsAsync();
            SelectedRelaySwimEvent =
                RelaySwimEvents.FirstOrDefault(item => item.Value is SwimEvent se && se.Id == newSwimEvent.Id);
        }
    }
}
