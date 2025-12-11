using ServiceLayer.EventService;

namespace UI.ViewModels;

public class EventsViewModel : ViewModelBase
{
    private readonly IEventService _eventService;

    public EventsViewModel(IEventService eventService)
    {
        _eventService = eventService;
    }

    public string Title => "Events Page";
}

