using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EventService;
using UI.Services;
using UI.ViewModels.Generic;

namespace UI.ViewModels.Details;

public partial class AgeGroupDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly EventsByAgeGroupViewModel _eventsTable;
    private readonly IAgeGroupService _ageGroupService;
    
    public ViewModelBase EventsTable => _eventsTable;

    [ObservableProperty] private string? _title = string.Empty;

    public AgeGroupDetailsViewModel(IAgeGroupService ageGroupService, IEventService eventService)
    {
        _ageGroupService = ageGroupService;
        _eventsTable = new EventsByAgeGroupViewModel(eventService);
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int idValue)
            return;
        
        var ageGroup = _ageGroupService.FindAsync(idValue).Result;
        Title = ageGroup?.DisplayName;
        
        _eventsTable.SetAgeGroupId(idValue);
    }
}

