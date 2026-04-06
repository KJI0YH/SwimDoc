using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;
using UI.Views.Controls;

namespace UI.ViewModels.AddEdit;

public partial class EntryAddEditViewModel(
    int? id,
    IEntryService entryService,
    IAthleteService athleteService,
    IEventService eventService)
    : GenericAddEditViewModel<Entry, int?>(id, entryService), IAddEditContextAware
{
    private int? _contextAthleteId;
    private int? _contextEventId;
    private int? _contextClubId;
    private int? _contextSwimStyleId;
    private string _entryTimeText = string.Empty;

    [ObservableProperty] private ObservableCollection<SearchableItem> _athletes = new();
    [ObservableProperty] private SearchableItem? _selectedAthlete;
    [ObservableProperty] private SearchableItem? _selectedSwimEvent;
    [ObservableProperty] private ObservableCollection<SearchableItem> _swimEvents = new();

    public override string WindowTitle => IsAdd ? "Создание заявки" : "Редактирование заявки";

    public int? EntryTime
    {
        get => Entity.EntryTime;
        set
        {
            Entity.EntryTime = value;
            OnPropertyChanged();
            var formatted = FormatEntryTime(value);
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
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            var parsed = ParseEntryTimeFromDigits(digits);
            var formatted = FormatEntryTime(parsed);

            if (_entryTimeText != formatted)
            {
                _entryTimeText = formatted;
                OnPropertyChanged();
            }

            if (Entity.EntryTime != parsed)
            {
                Entity.EntryTime = parsed;
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

    protected override async Task<Entry?> LoadEntityAsync(int? id)
    {
        return await CrudService.Query()
            .Include(entry => entry.Athlete)
            .Include(entry => entry.Relay)
            .Include(entry => entry.SwimEvent)
            .Include(entry => entry.SwimStyle)
            .FirstOrDefaultAsync(entry => entry.Id == id);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        LoadEvents();
        LoadAthletes();
        _entryTimeText = FormatEntryTime(Entity.EntryTime);
        OnPropertyChanged(nameof(EntryTimeText));
        SelectedAthlete = Athletes.FirstOrDefault(item => item.Value is Athlete a && a.Id == Entity.AthleteId);
        SelectedSwimEvent = Entity.SwimEventId == null
            ? SwimEvents.FirstOrDefault(item => item.Value == null)
            : SwimEvents.FirstOrDefault(item => item.Value is SwimEvent se && se.Id == Entity.SwimEventId);

        if (IsAdd)
        {
            if (_contextAthleteId.HasValue)
                SelectedAthlete = Athletes.FirstOrDefault(item => item.Value is Athlete a && a.Id == _contextAthleteId.Value);

            if (_contextEventId.HasValue)
                SelectedSwimEvent = SwimEvents.FirstOrDefault(item => item.Value is SwimEvent se && se.Id == _contextEventId.Value);
        }
    }

    public void ApplyContext(AddEditContext context)
    {
        _contextAthleteId = context.AthleteId;
        _contextEventId = context.EventId;
        _contextClubId = context.ClubId;
        _contextSwimStyleId = context.SwimStyleId;
    }

    private static string FormatEntryTime(int? value)
    {
        if (!value.HasValue)
            return string.Empty;

        var totalHundredths = value.Value;
        if (totalHundredths < 0)
            totalHundredths = 0;

        var minutes = totalHundredths / 6000;
        var seconds = (totalHundredths % 6000) / 100;
        var hundredths = totalHundredths % 100;

        return minutes > 0
            ? $"{minutes}:{seconds:D2}.{hundredths:D2}"
            : $"{seconds}.{hundredths:D2}";
    }

    private static int? ParseEntryTimeFromDigits(string digits)
    {
        if (string.IsNullOrWhiteSpace(digits))
            return null;

        // Guard from overflow while still allowing long minute values.
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

    private void LoadAthletes()
    {
        var query = athleteService.Query();
        if (_contextAthleteId.HasValue)
            query = query.Where(a => a.Id == _contextAthleteId.Value);
        else if (_contextClubId.HasValue)
            query = query.Where(a => a.ClubId == _contextClubId.Value);

        var athletes = query.ToList();
        Athletes.Clear();
        foreach (var athlete in athletes)
            Athletes.Add(new SearchableItem { Value = athlete, DisplayText = athlete.DisplayName });
    }

    private void LoadEvents()
    {
        IQueryable<SwimEvent> query = eventService.Query()
            .Include(se => se.AgeGroup)
            .Include(se => se.SwimStyle);

        if (_contextEventId.HasValue)
            query = query.Where(se => se.Id == _contextEventId.Value);
        else if (_contextSwimStyleId.HasValue)
            query = query.Where(se => se.SwimStyleId == _contextSwimStyleId.Value);

        var swimEvents = query.ToList();
        SwimEvents.Clear();
        foreach (var swimEvent in swimEvents)
            SwimEvents.Add(new SearchableItem { Value = swimEvent, DisplayText = swimEvent.DisplayName });
    }

    partial void OnSelectedAthleteChanged(SearchableItem? item)
    {
        if (item?.Value is not Athlete athlete) return;
        Entity.AthleteId = athlete.Id;
    }

    partial void OnSelectedSwimEventChanged(SearchableItem? item)
    {
        if (item?.Value is not SwimEvent swimEvent) return;
        Entity.SwimEventId = swimEvent.Id;
        Entity.SwimStyleId = swimEvent.SwimStyleId;
    }

    [RelayCommand]
    private void CreateAthlete()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var window = factory.CreateAndShowAndReturn<AthleteAddEditWindow>();
        if (window is { DialogResult: true, DataContext: IAddEditWindowResult { SavedEntity: Athlete newAthlete } })
        {
            LoadAthletes();
            SelectedAthlete = Athletes.FirstOrDefault(item => item.Value is Athlete a && a.Id == newAthlete.Id);
        }
    }

    [RelayCommand]
    private void CreateSwimEvent()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var window = factory.CreateAndShowAndReturn<EventAddEditWindow>();
        if (window is { DialogResult: true, DataContext: IAddEditWindowResult { SavedEntity: SwimEvent newSwimEvent } })
        {
            LoadEvents();
            SelectedSwimEvent =
                SwimEvents.FirstOrDefault(item => item.Value is SwimEvent se && se.Id == newSwimEvent.Id);
        }
    }
}