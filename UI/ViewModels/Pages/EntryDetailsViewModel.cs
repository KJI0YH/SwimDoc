using CommunityToolkit.Mvvm.ComponentModel;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.Helpers.Display;
using UI.Helpers.Threading;
using UI.Services.Navigation;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class EntryDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly IEntryService _entryService;
    private readonly HeatByEntryIdViewModel _heatTable;
    private int _titleEntityId;
    [ObservableProperty] private string? _title = string.Empty;

    public EntryDetailsViewModel(
        IEntryService entryService,
        IEventService eventService,
        IHeatService heatService,
        INavigationService navigationService)
    {
        _entryService = entryService;
        _heatTable = new HeatByEntryIdViewModel(eventService, heatService, navigationService);
    }

    public ViewModelBase HeatTable => _heatTable;
    public void OnNavigatedTo(object? parameter)
    {
        if (NavigationContext.Parse(parameter)?.ResolveId() is not int idValue)
            return;
        _heatTable.SetEntryId(idValue);
        LoadTitle(idValue);
    }

    private void LoadTitle(int id)
    {
        _titleEntityId = id;
        Title = string.Empty;
        _ = LoadTitleAsync(id);
    }

    private async Task LoadTitleAsync(int id)
    {
        var title = await DetailPageTitleLoader.LoadEntryAsync(_entryService, id).ConfigureAwait(false);
        await DispatcherUiHelper.InvokeOnUiAsync(() =>
        {
            if (_titleEntityId == id)
                Title = title;
        }).ConfigureAwait(false);
    }
}
