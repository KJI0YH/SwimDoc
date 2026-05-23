using System.Collections.ObjectModel;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using ServiceLayer.SwimStyleService;
using UI.Services;
using UI.ViewModels.Pages;
using UI.Views.Windows.AddEdit;

namespace UI.ViewModels.Pages.Data;

public class ClubsByIdViewModel : ClubsViewModel
{
    private int? _clubId;

    public ClubsByIdViewModel(IClubService clubService, IAthleteService athleteService)
        : base(clubService, athleteService)
    {
    }

    public void SetClubId(int? clubId)
    {
        _clubId = clubId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Club> ApplySearch(IQueryable<Club> query)
    {
        query = base.ApplySearch(query);
        return _clubId.HasValue ? query.Where(c => c.Id == _clubId.Value) : query.Where(_ => false);
    }
}

public class AthletesByIdViewModel : AthletesViewModel
{
    private int? _athleteId;

    public AthletesByIdViewModel(IAthleteService athleteService) : base(athleteService)
    {
    }

    public void SetAthleteId(int? athleteId)
    {
        _athleteId = athleteId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Athlete> ApplyQuery(IQueryable<Athlete> query)
    {
        query = base.ApplyQuery(query);
        return _athleteId.HasValue ? query.Where(a => a.Id == _athleteId.Value) : query.Where(_ => false);
    }
}

public class AthletesByClubViewModel : AthletesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _clubId;

    public AthletesByClubViewModel(IAthleteService athleteService) : base(athleteService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetClubId(int? clubId)
    {
        _clubId = clubId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Athlete> ApplyQuery(IQueryable<Athlete> query)
    {
        query = base.ApplyQuery(query);
        return _clubId.HasValue ? query.Where(a => a.ClubId == _clubId.Value) : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _clubId.HasValue ? new AddEditContext { ClubId = _clubId.Value } : null;
        var result = _windowFactory.CreateAndShow<AthleteAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}

public class EntriesByAthleteViewModel : EntriesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _athleteId;

    public EntriesByAthleteViewModel(IEntryService entryService, IEntryDocumentReaderService entryDocumentReaderService)
        : base(entryService, entryDocumentReaderService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetAthleteId(int? athleteId)
    {
        _athleteId = athleteId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        query = base.ApplyQuery(query);
        return _athleteId.HasValue
            ? query.Where(e =>
                e.AthleteId == _athleteId.Value ||
                (e.Relay != null && e.Relay.Positions.Any(p => p.AthleteId == _athleteId.Value)))
            : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _athleteId.HasValue ? new AddEditContext { AthleteId = _athleteId.Value } : null;
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}

public class EntriesByClubViewModel : EntriesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _clubId;

    public EntriesByClubViewModel(IEntryService entryService, IEntryDocumentReaderService entryDocumentReaderService)
        : base(entryService, entryDocumentReaderService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetClubId(int? clubId)
    {
        _clubId = clubId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        query = base.ApplyQuery(query);
        return _clubId.HasValue
            ? query.Where(e =>
                (e.Athlete != null && e.Athlete.ClubId == _clubId.Value) ||
                (e.Relay != null && e.Relay.ClubId == _clubId.Value))
            : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _clubId.HasValue ? new AddEditContext { ClubId = _clubId.Value } : null;
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}

public class EntriesByEventViewModel : EntriesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _eventId;

    public EntriesByEventViewModel(IEntryService entryService, IEntryDocumentReaderService entryDocumentReaderService)
        : base(entryService, entryDocumentReaderService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetEventId(int? eventId)
    {
        _eventId = eventId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        query = base.ApplyQuery(query);
        return _eventId.HasValue ? query.Where(e => e.SwimEventId == _eventId.Value) : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _eventId.HasValue ? new AddEditContext { EventId = _eventId.Value } : null;
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}

public class HeatsByAthleteViewModel : HeatsViewModel
{
    private int? _athleteId;

    public HeatsByAthleteViewModel(IEventService eventService, IHeatService heatService) : base(eventService,
        heatService)
    {
    }


    public void SetAthleteId(int? athleteId)
    {
        _athleteId = athleteId;
        LoadDataCommand.Execute(null);
    }

    protected override async Task LoadHeatPositionsAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId || !_athleteId.HasValue)
        {
            HeatPositions = [];
            return;
        }

        IsLoading = true;
        try
        {
            var heats = await HeatService.GetHeatsByEventIdAsync(eventId);
            var heatsInEvent = heats.Count;
            var heatsForAthlete = heats
                .Where(heat => heat.Positions.Any(hp =>
                    hp.Entry.AthleteId == _athleteId.Value ||
                    (hp.Entry.Relay != null && hp.Entry.Relay.Positions.Any(p => p.AthleteId == _athleteId.Value))))
                .ToList();
            var heatsTotal = HeatService.GetTotalHeats();
            var heatPositionViews = heatsForAthlete.SelectMany(h =>
                h.Positions.Select(p =>
                    new HeatPositionView(p, h.Number, heatsInEvent, h.Order, heatsTotal, h.Status, h.DisplayDayTime)));
            HeatPositions = new ObservableCollection<HeatPositionView>(heatPositionViews);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _athleteId.HasValue
            ? query.Select(se => new SwimEvent
                {
                    Id = se.Id,
                    Date = se.Date,
                    Time = se.Time,
                    Order = se.Order,
                    AgeGroup = se.AgeGroup,
                    SwimStyle = se.SwimStyle,
                    LaneMin = se.LaneMin,
                    LaneMax = se.LaneMax,

                    Heats = se.Heats
                        .Where(heat => heat.Positions.Any(hp =>
                            hp.Entry.AthleteId == _athleteId.Value ||
                            (hp.Entry.Relay != null &&
                             hp.Entry.Relay.Positions.Any(p => p.AthleteId == _athleteId.Value))))
                        .ToList()
                })
                .Where(se => se.Heats.Any())
            : query.Where(_ => false);
    }
}

public class HeatsByEventViewModel : HeatsViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _eventId;

    public HeatsByEventViewModel(IEventService eventService, IHeatService heatService) : base(eventService, heatService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetEventId(int? eventId)
    {
        _eventId = eventId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _eventId.HasValue
            ? query.Where(se => se.Id == _eventId.Value)
            : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _eventId.HasValue ? new AddEditContext { EventId = _eventId.Value } : null;
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id, context);
        if (result == true)
        {
            _ = LoadDataAsync();
            _ = RefreshAsync();
        }
    }
}

public class FixationByEventViewModel(
    IEventService eventService,
    IHeatService heatService,
    IPointScoreProvider pointScoreProvider)
    : FixationViewModel(eventService, heatService, pointScoreProvider)
{
    private int? _eventId;

    public void SetEventId(int? eventId)
    {
        _eventId = eventId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _eventId.HasValue
            ? query.Where(se => se.Id == _eventId.Value)
            : query.Where(_ => false);
    }
}

public class ResultsByEventViewModel(IEventService eventService, IEntryService entryService)
    : ResultsViewModel(eventService, entryService)
{
    private int? _eventId;

    public Task RefreshForEventAsync(int eventId) => LoadEntriesForEventIdAsync(eventId);

    public void SetEventId(int? eventId)
    {
        _eventId = eventId;
        SelectedSwimEvent = null;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _eventId.HasValue
            ? query.Where(se => se.Id == _eventId.Value)
            : query.Where(_ => false);
    }
}

public class EntriesBySwimStyleViewModel : EntriesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _swimStyleId;

    public EntriesBySwimStyleViewModel(IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService)
        : base(entryService, entryDocumentReaderService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetSwimStyleId(int? swimStyleId)
    {
        _swimStyleId = swimStyleId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        query = base.ApplyQuery(query);
        return _swimStyleId.HasValue ? query.Where(e => e.SwimStyleId == _swimStyleId.Value) : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _swimStyleId.HasValue ? new AddEditContext { SwimStyleId = _swimStyleId.Value } : null;
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}

public class HeatByEntryIdViewModel : HeatsViewModel
{
    private int? _entryId;

    public HeatByEntryIdViewModel(IEventService eventService, IHeatService heatService) : base(eventService,
        heatService)

    {
    }

    public void SetEntryId(int? entryId)
    {
        _entryId = entryId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _entryId.HasValue
            ? query.Select(se => new SwimEvent
                {
                    Id = se.Id,
                    Date = se.Date,
                    Time = se.Time,
                    Order = se.Order,
                    AgeGroup = se.AgeGroup,
                    SwimStyle = se.SwimStyle,
                    LaneMin = se.LaneMin,
                    LaneMax = se.LaneMax,

                    Heats = se.Heats
                        .Where(heat => heat.Positions
                            .Any(hp => hp.EntryId == _entryId.Value)
                        )
                        .ToList()
                })
                .Where(se => se.Heats.Any())
            : query.Where(_ => false);
    }
}

public class EventsByIdViewModel : EventsViewModel
{
    private int? _eventId;

    public EventsByIdViewModel(IEventService eventService) : base(eventService)
    {
    }

    public void SetEventId(int? eventId)
    {
        _eventId = eventId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _eventId.HasValue ? query.Where(e => e.Id == _eventId.Value) : query.Where(_ => false);
    }
}

public class EventsByAgeGroupViewModel : EventsViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _ageGroupId;

    public EventsByAgeGroupViewModel(IEventService eventService) : base(eventService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetAgeGroupId(int? ageGroupId)
    {
        _ageGroupId = ageGroupId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _ageGroupId.HasValue ? query.Where(e => e.AgeGroupId == _ageGroupId.Value) : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _ageGroupId.HasValue ? new AddEditContext { AgeGroupId = _ageGroupId.Value } : null;
        var result = _windowFactory.CreateAndShow<EventAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}

public class EventsBySwimStyleViewModel : EventsViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _swimStyleId;

    public EventsBySwimStyleViewModel(IEventService eventService) : base(eventService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetSwimStyleId(int? swimStyleId)
    {
        _swimStyleId = swimStyleId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _swimStyleId.HasValue ? query.Where(e => e.SwimStyleId == _swimStyleId.Value) : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _swimStyleId.HasValue ? new AddEditContext { SwimStyleId = _swimStyleId.Value } : null;
        var result = _windowFactory.CreateAndShow<EventAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}

public class AgeGroupsByIdViewModel : AgeGroupsViewModel
{
    private int? _ageGroupId;

    public AgeGroupsByIdViewModel(IAgeGroupService ageGroupService) : base(ageGroupService)
    {
    }

    public void SetAgeGroupId(int? ageGroupId)
    {
        _ageGroupId = ageGroupId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<AgeGroup> ApplySearch(IQueryable<AgeGroup> query)
    {
        query = base.ApplySearch(query);
        return _ageGroupId.HasValue ? query.Where(ag => ag.Id == _ageGroupId.Value) : query.Where(_ => false);
    }
}

public class SwimStylesByIdViewModel : SwimStylesViewModel
{
    private int? _swimStyleId;

    public SwimStylesByIdViewModel(ISwimStyleService swimStyleService) : base(swimStyleService)
    {
    }

    public void SetSwimStyleId(int? swimStyleId)
    {
        _swimStyleId = swimStyleId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimStyle> ApplySearch(IQueryable<SwimStyle> query)
    {
        query = base.ApplySearch(query);
        return _swimStyleId.HasValue ? query.Where(ss => ss.Id == _swimStyleId.Value) : query.Where(_ => false);
    }
}