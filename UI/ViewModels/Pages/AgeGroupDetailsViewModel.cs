using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EventService;
using UI.Services;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class AgeGroupDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly IAgeGroupService _ageGroupService;
    private readonly EventsByAgeGroupViewModel _eventsTable;

    [ObservableProperty] private string? _title = string.Empty;

    public AgeGroupDetailsViewModel(IAgeGroupService ageGroupService, IEventService eventService)
    {
        _ageGroupService = ageGroupService;
        _eventsTable = new EventsByAgeGroupViewModel(eventService);
    }

    public ViewModelBase EventsTable => _eventsTable;

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int idValue)
            return;

        _eventsTable.SetAgeGroupId(idValue);
    }
}