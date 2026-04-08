using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.Services;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public class EntryDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly HeatByEntryIdViewModel _heatTable;

    public EntryDetailsViewModel(IEventService eventService, IHeatService heatService)
    {
        _heatTable = new HeatByEntryIdViewModel(eventService, heatService);
    }

    public ViewModelBase HeatTable => _heatTable;

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int idValue)
            return;

        _heatTable.SetEntryId(idValue);
    }
}