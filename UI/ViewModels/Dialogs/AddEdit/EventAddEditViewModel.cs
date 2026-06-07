using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer;
using DataLayer.Display;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EventService;
using ServiceLayer.SwimStyleService;
using UI.Resources;
using UI.Models;
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Dialogs.AddEdit;

public partial class EventAddViewModel(
    int? id,
    IEventService eventService,
    IAgeGroupService ageGroupService,
    ISwimStyleService swimStyleService)
    : AddEditViewModel<SwimEvent, int?>(id, eventService), INavigationContextAware
{
    [ObservableProperty] private ObservableCollection<SearchableItem> _ageGroups = new();
    private int? _contextAgeGroupId;
    private int? _contextSwimStyleId;
    [ObservableProperty] private ObservableCollection<SearchableItem> _previousSwimEvents = new();
    [ObservableProperty] private SearchableItem? _selectedAgeGroup;
    [ObservableProperty] private SearchableItem? _selectedPreviousSwimEvent;
    [ObservableProperty] private SearchableItem? _selectedSwimStyle;
    [ObservableProperty] private ObservableCollection<SearchableItem> _swimStyles = new();
    [ObservableProperty] private int _laneTabIndex;
    private string? _savedCustomLaneNames;
    private bool _isSyncingLaneTab;
    public IReadOnlyList<string> HourOptions { get; } = CreateTimePartOptions(24);
    public IReadOnlyList<string> MinuteOptions { get; } = CreateTimePartOptions(60);
    public override string WindowTitle => IsAdd ? Strings.WindowTitle_CreateEvent : Strings.WindowTitle_EditEvent;
    public int Order
    {
        get => Entity.Order;
        set
        {
            Entity.Order = value;
            OnPropertyChanged();
        }
    }

    public DateOnly Date
    {
        get => Entity.Date;
        set
        {
            Entity.Date = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedDate));
        }
    }

    public DateTime? SelectedDate
    {
        get => Date.ToDateTime(TimeOnly.MinValue);
        set
        {
            if (!value.HasValue) return;
            Date = DateOnly.FromDateTime(value.Value);
        }
    }

    public TimeOnly? Time
    {
        get => Entity.Time;
        set
        {
            Entity.Time = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HourText));
            OnPropertyChanged(nameof(MinuteText));
        }
    }

    public string HourText
    {
        get => Entity.Time?.Hour.ToString("00") ?? string.Empty;
        set
        {
            if (!TryParseTimePart(value, 23, out var hour))
                return;
            UpdateTime(hour, Entity.Time?.Minute);
        }
    }

    public string MinuteText
    {
        get => Entity.Time?.Minute.ToString("00") ?? string.Empty;
        set
        {
            if (!TryParseTimePart(value, 59, out var minute))
                return;
            UpdateTime(Entity.Time?.Hour, minute);
        }
    }

    public string DateString
    {
        get => Entity.Date.ToString("dd.MM.yyyy");
        set
        {
            if (DateOnly.TryParse(value, out var date))
            {
                Entity.Date = date;
                OnPropertyChanged(nameof(Date));
                OnPropertyChanged(nameof(SelectedDate));
            }
        }
    }

    private void UpdateTime(int? hour, int? minute)
    {
        if (!hour.HasValue && !minute.HasValue)
            Entity.Time = null;
        else
            Entity.Time = new TimeOnly(hour ?? 0, minute ?? 0);
        OnPropertyChanged(nameof(Time));
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

    public Course Course
    {
        get => Entity.Course;
        set
        {
            Entity.Course = value;
            OnPropertyChanged();
        }
    }

    public EventRound Round
    {
        get => Entity.Round;
        set
        {
            Entity.Round = value;
            OnPropertyChanged();
        }
    }

    public int? RoundParticipantsCount
    {
        get => Entity.RoundParticipantsCount;
        set
        {
            Entity.RoundParticipantsCount = value;
            OnPropertyChanged();
        }
    }

    public int LaneMin
    {
        get => Entity.LaneMin;
        set
        {
            Entity.LaneMin = value;
            OnPropertyChanged();
        }
    }

    public int LaneMax
    {
        get => Entity.LaneMax;
        set
        {
            Entity.LaneMax = value;
            OnPropertyChanged();
        }
    }

    public string? CustomLaneNames
    {
        get => Entity.CustomLaneNames;
        set
        {
            Entity.CustomLaneNames = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (LaneTabIndex == 1)
                _savedCustomLaneNames = Entity.CustomLaneNames;
            OnPropertyChanged();
        }
    }

    public Array CourseValues => Enum.GetValues<Course>();
    public Array EventRoundValues => Enum.GetValues<EventRound>();
    public void ApplyContext(NavigationContext context)
    {
        _contextAgeGroupId = context.AgeGroupId;
        _contextSwimStyleId = context.SwimStyleId;
    }

    protected override async Task<SwimEvent?> LoadEntityAsync(int? id)
    {
        return await CrudService.Query()
            .Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle)
            .Include(swimEvent => swimEvent.PreviousSwimEvent)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        OnPropertyChanged(nameof(HourText));
        OnPropertyChanged(nameof(MinuteText));
        LoadAgeGroups();
        LoadSwimStyles();
        LoadPreviousSwimEvents();
        if (IsEdit)
        {
            SelectedAgeGroup =
                Enumerable.FirstOrDefault<SearchableItem>(AgeGroups, item => item.Value is AgeGroup ag && ag.Id == Entity.AgeGroupId);
            SelectedSwimStyle =
                Enumerable.FirstOrDefault<SearchableItem>(SwimStyles, item => item.Value is SwimStyle ss && ss.Id == Entity.SwimStyleId);
            SelectedPreviousSwimEvent = Enumerable.FirstOrDefault<SearchableItem>(PreviousSwimEvents, item =>
                item.Value is SwimEvent se && se.Id == Entity.PreviousSwimEventId);
        }
        else
        {
            Order = eventService.GetNextOrderNumber();
            Date = eventService.GetPreviousDate();
            Time = eventService.GetPreviousTime();
            Course = eventService.GetPreviousCourse();
            var previousLanes = eventService.GetPreviousLaneSettings();
            (LaneMin, LaneMax) = (previousLanes.min, previousLanes.max);
            CustomLaneNames = previousLanes.customLaneNames;
            SelectedPreviousSwimEvent = Enumerable.FirstOrDefault<SearchableItem>(PreviousSwimEvents, item => item.Value == null);
            if (_contextAgeGroupId.HasValue)
                SelectedAgeGroup =
                    Enumerable.FirstOrDefault<SearchableItem>(AgeGroups, item => item.Value is AgeGroup ag && ag.Id == _contextAgeGroupId.Value);
            if (_contextSwimStyleId.HasValue)
                SelectedSwimStyle = Enumerable.FirstOrDefault<SearchableItem>(SwimStyles, item =>
                    item.Value is SwimStyle ss && ss.Id == _contextSwimStyleId.Value);
        }
        InitializeLaneTab();
    }

    protected override bool ValidateBeforeSave()
    {
        if (LaneTabIndex == 1)
        {
            if (string.IsNullOrWhiteSpace(Entity.CustomLaneNames))
            {
                ValidationErrors.Add(Strings.Event_Validation_CustomLaneNamesRequired);
                return false;
            }
        }
        else
            Entity.CustomLaneNames = null;
        return true;
    }

    partial void OnLaneTabIndexChanged(int value) => ApplyLaneTabIndex(value);
    private void InitializeLaneTab()
    {
        _isSyncingLaneTab = true;
        try
        {
            _savedCustomLaneNames = Entity.CustomLaneNames;
            LaneTabIndex = SwimEventLaneNames.HasCustomLaneNames(Entity) ? 1 : 0;
            if (LaneTabIndex == 0)
                Entity.CustomLaneNames = null;
            OnPropertyChanged(nameof(CustomLaneNames));
        }
        finally
        {
            _isSyncingLaneTab = false;
        }
    }

    private void ApplyLaneTabIndex(int tabIndex)
    {
        if (_isSyncingLaneTab || Entity is null)
            return;
        if (tabIndex == 0)
        {
            _savedCustomLaneNames = Entity.CustomLaneNames;
            Entity.CustomLaneNames = null;
        }
        else
            Entity.CustomLaneNames = _savedCustomLaneNames;
        OnPropertyChanged(nameof(CustomLaneNames));
    }

    partial void OnSelectedAgeGroupChanged(SearchableItem? value)
    {
        if (value?.Value is not AgeGroup ageGroup) return;
        Entity.AgeGroupId = ageGroup.Id;
    }

    partial void OnSelectedSwimStyleChanged(SearchableItem? value)
    {
        if (value?.Value is not SwimStyle swimStyle) return;
        Entity.SwimStyleId = swimStyle.Id;
    }

    partial void OnSelectedPreviousSwimEventChanged(SearchableItem? value)
    {
        if (value?.Value is not SwimEvent swimEvent) return;
        Entity.PreviousSwimEventId = swimEvent.Id;
    }

    private void LoadAgeGroups()
    {
        var query = ageGroupService.Query();
        if (_contextAgeGroupId.HasValue)
            query = query.Where(ag => ag.Id == _contextAgeGroupId.Value);
        var ageGroups = query.ToList();
        AgeGroups.Clear();
        foreach (var ageGroup in ageGroups)
            AgeGroups.Add(new SearchableItem
            {
                Value = ageGroup,
                DisplayText = EntityDisplayFormatter.FormatAgeGroup(ageGroup)
            });
    }

    private void LoadSwimStyles()
    {
        var query = swimStyleService.Query();
        if (_contextSwimStyleId.HasValue)
            query = query.Where(ss => ss.Id == _contextSwimStyleId.Value);
        var swimStyles = query.ToList();
        SwimStyles.Clear();
        foreach (var swimStyle in swimStyles)
            SwimStyles.Add(new SearchableItem
            {
                Value = swimStyle,
                DisplayText = EntityDisplayFormatter.FormatSwimStyle(swimStyle)
            });
    }

    private void LoadPreviousSwimEvents()
    {
        var swimEvents = eventService.Query()
            .Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle)
            .Include(swimEvent => swimEvent.PreviousSwimEvent)
            .ToList();
        PreviousSwimEvents.Clear();
        PreviousSwimEvents.Add(new SearchableItem { Value = null, DisplayText = Strings.Common_NoneParen });
        foreach (var swimEvent in swimEvents)
        {
            if (!IsAdd && swimEvent.Id == Entity.Id)
                continue;
            PreviousSwimEvents.Add(new SearchableItem
            {
                Value = swimEvent,
                DisplayText = EntityDisplayFormatter.FormatSwimEvent(swimEvent)
            });
        }
    }

    [RelayCommand]
    private void CreateNewAgeGroup()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var dialog = factory.CreateAndShowAndReturn<AgeGroupAddEditWindow>();
        if (dialog.DialogResult == true && dialog.DataContext is IWindowResult result &&
            result.Result is AgeGroup newAgeGroup)
        {
            LoadAgeGroups();
            SelectedAgeGroup = Enumerable.FirstOrDefault<SearchableItem>(AgeGroups, item => item.Value is AgeGroup ag && ag.Id == newAgeGroup.Id);
        }
    }

    [RelayCommand]
    private void CreateNewSwimStyle()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var dialog = factory.CreateAndShowAndReturn<SwimStyleAddEditWindow>();
        if (dialog is { DialogResult: true, DataContext: IWindowResult { Result: SwimStyle newSwimStyle } })
        {
            LoadSwimStyles();
            SelectedSwimStyle =
                Enumerable.FirstOrDefault<SearchableItem>(SwimStyles, item => item.Value is SwimStyle ss && ss.Id == newSwimStyle.Id);
        }
    }

    [RelayCommand]
    private void CreateNewSwimEvent()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var dialog = factory.CreateAndShowAndReturn<EventAddEditWindow>();
        if (dialog is { DialogResult: true, DataContext: IWindowResult { Result: SwimEvent newSwimEvent } })
        {
            LoadPreviousSwimEvents();
            SelectedPreviousSwimEvent =
                Enumerable.FirstOrDefault<SearchableItem>(PreviousSwimEvents, item => item.Value is SwimEvent se && se.Id == newSwimEvent.Id);
        }
    }
}
