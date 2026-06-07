using CommunityToolkit.Mvvm.ComponentModel;
using UI.Resources;
using Wpf.Ui.Controls;

namespace UI.ViewModels.Pages;

public sealed partial class PagingSettingItemViewModel : ObservableObject
{
    private readonly IPagingSettingsService _pagingSettings;
    public PagingPage Page { get; }
    public string Title { get; }
    public string Description => Strings.Settings_Paging_PageSize;
    public SymbolRegular IconSymbol => Page switch
    {
        PagingPage.Events => SymbolRegular.CalendarLtr24,
        PagingPage.Entries => SymbolRegular.DocumentCopy24,
        PagingPage.Heats => SymbolRegular.SwimmingPool24,
        PagingPage.Athletes => SymbolRegular.People24,
        PagingPage.Clubs => SymbolRegular.BuildingTownhouse24,
        PagingPage.AgeGroups => SymbolRegular.GroupList24,
        PagingPage.SwimStyles => SymbolRegular.Accessibility24,
        _ => SymbolRegular.Document24
    };
    [ObservableProperty] private int _pageSize;
    public PagingSettingItemViewModel(IPagingSettingsService pagingSettings, PagingPage page)
    {
        _pagingSettings = pagingSettings;
        Page = page;
        Title = PagingSettingsService.GetPageTitle(page);
        _pageSize = pagingSettings.GetPageSize(page);
        _pagingSettings.PageSizeChanged += OnPagingSettingsChanged;
    }

    partial void OnPageSizeChanged(int value)
    {
        var normalized = _pagingSettings.SetPageSize(Page, value);
        if (normalized != value)
            PageSize = normalized;
    }

    private void OnPagingSettingsChanged(PagingPage page)
    {
        if (page != Page)
            return;
        var size = _pagingSettings.GetPageSize(Page);
        if (size != PageSize)
            PageSize = size;
    }
}
