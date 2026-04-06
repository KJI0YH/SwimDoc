using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EventService;
using ServiceLayer.SwimStyleService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;
using UI.Views.Controls;

namespace UI.ViewModels.AddEdit;

public partial class EventAddAddEditViewModel(
    int? id,
    IEventService eventService,
    IAgeGroupService ageGroupService,
    ISwimStyleService swimStyleService)
    : GenericAddEditViewModel<SwimEvent, int?>(id, eventService), IAddEditContextAware
{
    private int? _contextAgeGroupId;
    private int? _contextSwimStyleId;
    [ObservableProperty] private ObservableCollection<SearchableItem> _ageGroups = new();

    [ObservableProperty] private ObservableCollection<SearchableItem> _previousSwimEvents = new();

    [ObservableProperty] private SearchableItem? _selectedAgeGroup;

    [ObservableProperty] private SearchableItem? _selectedPreviousSwimEvent;

    [ObservableProperty] private SearchableItem? _selectedSwimStyle;

    [ObservableProperty] private ObservableCollection<SearchableItem> _swimStyles = new();

    public override string WindowTitle => IsAdd ? "Создание события" : "Редактирование события";

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
            OnPropertyChanged(nameof(TimeString));
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

    public string? TimeString
    {
        get => Entity.Time?.ToString("HH:mm:ss");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                Entity.Time = null;
            else if (TimeOnly.TryParse(value, out var time)) Entity.Time = time;

            OnPropertyChanged(nameof(Time));
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

    public Array EventRoundValues => Enum.GetValues<EventRound>();

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
        LoadAgeGroups();
        LoadSwimStyles();
        LoadPreviousSwimEvents();

        if (IsEdit)
        {
            SelectedAgeGroup = AgeGroups.FirstOrDefault(item => item.Value is AgeGroup ag && ag.Id == Entity.AgeGroupId);
            SelectedSwimStyle = SwimStyles.FirstOrDefault(item => item.Value is SwimStyle ss && ss.Id == Entity.SwimStyleId);
            SelectedPreviousSwimEvent = PreviousSwimEvents.FirstOrDefault(item => item.Value is SwimEvent se && se.Id == Entity.PreviousSwimEventId);
        }
        else
        {
            Date = DateOnly.FromDateTime(DateTime.Today);
            Order = eventService.GetNextOrderNumber();
            SelectedPreviousSwimEvent = PreviousSwimEvents.FirstOrDefault(item => item.Value == null);
            if (_contextAgeGroupId.HasValue)
                SelectedAgeGroup = AgeGroups.FirstOrDefault(item => item.Value is AgeGroup ag && ag.Id == _contextAgeGroupId.Value);
            if (_contextSwimStyleId.HasValue)
                SelectedSwimStyle = SwimStyles.FirstOrDefault(item => item.Value is SwimStyle ss && ss.Id == _contextSwimStyleId.Value);
        }
    }

    public void ApplyContext(AddEditContext context)
    {
        _contextAgeGroupId = context.AgeGroupId;
        _contextSwimStyleId = context.SwimStyleId;
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
                DisplayText = ageGroup.DisplayName
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
                DisplayText = swimStyle.DisplayName
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
        PreviousSwimEvents.Add(new SearchableItem { Value = null, DisplayText = "(Нет)" });

        foreach (var swimEvent in swimEvents)
        {
            if (!IsAdd && swimEvent.Id == Entity.Id)
                continue;

            PreviousSwimEvents.Add(new SearchableItem
            {
                Value = swimEvent,
                DisplayText = swimEvent.DisplayName
            });
        }
    }

    [RelayCommand]
    private void CreateNewAgeGroup()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var window = factory.CreateAndShowAndReturn<AgeGroupAddEditWindow>();
        if (window.DialogResult == true && window.DataContext is IAddEditWindowResult result &&
            result.SavedEntity is AgeGroup newAgeGroup)
        {
            LoadAgeGroups();
            SelectedAgeGroup = AgeGroups.FirstOrDefault(item => item.Value is AgeGroup ag && ag.Id == newAgeGroup.Id);
        }
    }

    [RelayCommand]
    private void CreateNewSwimStyle()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var window = factory.CreateAndShowAndReturn<SwimStyleAddEditWindow>();
        if (window is { DialogResult: true, DataContext: IAddEditWindowResult { SavedEntity: SwimStyle newSwimStyle } })
        {
            LoadSwimStyles();
            SelectedSwimStyle =
                SwimStyles.FirstOrDefault(item => item.Value is SwimStyle ss && ss.Id == newSwimStyle.Id);
        }
    }

    [RelayCommand]
    private void CreateNewSwimEvent()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var window = factory.CreateAndShowAndReturn<EventAddEditWindow>();
        if (window is { DialogResult: true, DataContext: IAddEditWindowResult { SavedEntity: SwimEvent newSwimEvent } })
        {
            LoadPreviousSwimEvents();
            SelectedPreviousSwimEvent =
                PreviousSwimEvents.FirstOrDefault(item => item.Value is SwimEvent se && se.Id == newSwimEvent.Id);
        }
    }
}