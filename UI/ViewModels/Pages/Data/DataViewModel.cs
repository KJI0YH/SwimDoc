using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using DataLayer.QueryObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Crud;
using ServiceLayer.Logging;
using UI.Helpers.Threading;
using UI.Resources;
using UI.Models.Rows;
using UI.Services.Navigation;
namespace UI.ViewModels.Pages.Data;

public partial class DataViewModel<TEntity, TRowView, TKey> : DataViewModelBase, IDataLoadable
    where TEntity : class
    where TRowView : IEntityRowView<TEntity>
{
    private readonly Type _crudServiceType;
    private readonly Func<ICrudService<TEntity, TKey>> _resolveCrudService;
    private IServiceProvider? _loadServiceProvider;
    protected IServiceProvider? LoadServiceProvider => _loadServiceProvider;
    private readonly INavigationService _navigationService;
    protected INavigationService NavigationService => _navigationService;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private bool _suppressPageLoad;
    private bool _needsLoad = true;
    [ObservableProperty] private bool _autoGenerateColumns = true;
    [ObservableProperty] private ObservableCollection<ColumnConfiguration> _columnConfigurations = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<TRowView> _items = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private TRowView? _selectedItem;
    [ObservableProperty] private ObservableCollection<TRowView> _selectedItems = new();
    [ObservableProperty] private string _sortColumn = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsInfo))]
    private int _currentPage = 0;
    [ObservableProperty] private ObservableCollection<int> _pageOptions = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsInfo))]
    private int _pageSize;
    public string ItemsInfo
    {
        get
        {
            if (TotalItems <= 0) return string.Format(Strings.Paging_ItemsInfo_EmptyFormat, TotalItems);
            var from = CurrentPage * PageSize + 1;
            var to = Math.Min(TotalItems, CurrentPage * PageSize + PageSize);
            return string.Format(Strings.Paging_ItemsInfo_RangeFormat, from, to, TotalItems);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPagingEnabled))]
    private int _totalPages;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsInfo))]
    private int _totalItems;
    [ObservableProperty] private ListSortDirection _sortDirection = ListSortDirection.Ascending;
    public bool IsPagingEnabled => TotalPages > 0;
    protected virtual bool UsesPaging => true;
    protected virtual PagingPage PagingSettingsPage => PagingPage.Entries;
    protected ICrudService<TEntity, TKey> CrudService => _resolveCrudService();

    public DataViewModel(ICrudService<TEntity, TKey> crudService)
    {
        var serviceType = ResolveServiceType(crudService);
        _crudServiceType = serviceType;
        _resolveCrudService = () => (ICrudService<TEntity, TKey>)App.Current.Services.GetRequiredService(serviceType);
        _navigationService = App.Current.Services.GetRequiredService<INavigationService>();
        var databaseConnection = App.Current.Services.GetRequiredService<IDatabaseConnection>();
        databaseConnection.ConnectionChanged += OnConnectionChanged;
        var pagingSettings = App.Current.Services.GetRequiredService<IPagingSettingsService>();
        pagingSettings.PageSizeChanged += OnPagingSettingsPageSizeChanged;
        if (UsesPaging)
            PageSize = pagingSettings.GetPageSize(PagingSettingsPage);
        InitializeColumns();
    }

    private static Type ResolveServiceType(ICrudService<TEntity, TKey> service)
    {
        var crudInterface = typeof(ICrudService<TEntity, TKey>);
        var specificInterface = service.GetType().GetInterfaces()
            .FirstOrDefault(i => i != crudInterface && crudInterface.IsAssignableFrom(i));
        return specificInterface ?? service.GetType();
    }

    private void OnConnectionChanged(string _)
    {
        ResetForNewCompetition();
        RequestReload();
    }

    public void EnsureDataLoaded()
    {
        if (!_needsLoad)
            return;
        _needsLoad = false;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            _ = LoadDataWithPrepareAsync();
            return;
        }
        dispatcher.BeginInvoke(() => _ = LoadDataWithPrepareAsync(), DispatcherPriority.Loaded);
    }

    public void RequestReload()
    {
        _needsLoad = true;
    }

    protected virtual Task PrepareBeforeLoadAsync() => Task.CompletedTask;

    protected ICrudService<TEntity, TKey> CreateScopedCrudService(IServiceProvider serviceProvider) =>
        (ICrudService<TEntity, TKey>)serviceProvider.GetRequiredService(_crudServiceType);

    private async Task BeginLoadAsync()
    {
        await _loadGate.WaitAsync().ConfigureAwait(false);
        await DispatcherUiHelper.InvokeOnUiAsync(() => IsLoading = true);
        await YieldLoadingUiAsync();
    }

    private async Task EndLoadAsync()
    {
        await DispatcherUiHelper.InvokeOnUiAsync(() => IsLoading = false);
        _loadGate.Release();
    }

    private async Task LoadDataWithPrepareAsync()
    {
        await BeginLoadAsync();
        try
        {
            await PrepareBeforeLoadAsync().ConfigureAwait(false);
            await LoadDataCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            await EndLoadAsync();
        }
    }

    protected virtual void ResetForNewCompetition()
    {
        SearchText = string.Empty;
        SortColumn = string.Empty;
        SortDirection = ListSortDirection.Ascending;
        CurrentPage = 0;
        SelectedItem = default;
        SelectedItems.Clear();
    }

    private void OnPagingSettingsPageSizeChanged(PagingPage page)
    {
        if (!UsesPaging || page != PagingSettingsPage)
            return;
        var pagingSettings = App.Current.Services.GetRequiredService<IPagingSettingsService>();
        var newSize = pagingSettings.GetPageSize(PagingSettingsPage);
        if (newSize == PageSize)
            return;
        PageSize = newSize;
        if (CurrentPage != 0)
            CurrentPage = 0;
        else
            LoadDataCommand.Execute(null);
    }

    public override ObservableCollection<ColumnConfiguration> GetColumnConfigurations()
    {
        return ColumnConfigurations;
    }

    public override bool GetAutoGenerateColumns()
    {
        return AutoGenerateColumns;
    }

    protected virtual void InitializeColumns()
    {
        AutoGenerateColumns = true;
    }

    protected virtual Task<List<TRowView>> LoadPageRowsAsync(IQueryable<TEntity> query) =>
        LoadPageRowsAsync(query, App.Current.Services);

    protected virtual async Task<List<TRowView>> LoadPageRowsAsync(
        IQueryable<TEntity> query,
        IServiceProvider serviceProvider)
    {
        var entities = await query.ToListAsync().ConfigureAwait(false);
        return entities.Select(CreateRowView).ToList();
    }

    protected virtual TRowView CreateRowView(TEntity entity) =>
        (TRowView)Activator.CreateInstance(typeof(TRowView), entity)!;

    partial void OnSelectedItemsChanged(ObservableCollection<TRowView> value)
    {
        EditItemCommand.NotifyCanExecuteChanged();
        DeleteItemCommand.NotifyCanExecuteChanged();
        OpenDetailsCommand.NotifyCanExecuteChanged();
    }

    public override void SyncSelectedItemsFromGrid(IList? gridSelection)
    {
        var list = new List<TRowView>();
        if (gridSelection != null)
            foreach (var item in gridSelection)
                if (item is TRowView row)
                    list.Add(row);
        SelectedItems = new ObservableCollection<TRowView>(list);
        SelectedItem = list.Count == 1 ? list[0] : default;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (CurrentPage == 0)
            LoadDataCommand.Execute(null);
        else
            CurrentPage = 0;
    }

    partial void OnCurrentPageChanged(int value)
    {
        GoToFirstPageCommand.NotifyCanExecuteChanged();
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
        GoToLastPageCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ItemsInfo));
        if (!_suppressPageLoad)
            LoadDataCommand.Execute(null);
    }

    partial void OnTotalPagesChanged(int value)
    {
        GoToFirstPageCommand.NotifyCanExecuteChanged();
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
        GoToLastPageCommand.NotifyCanExecuteChanged();
        PageOptions = value > 0
            ? new ObservableCollection<int>(Enumerable.Range(1, value))
            : new ObservableCollection<int>();
    }

    private bool CanLoadData() => !IsLoading;
    partial void OnIsLoadingChanged(bool value)
    {
        LoadDataCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanLoadData))]
    protected virtual async Task LoadDataAsync()
    {
        await BeginLoadAsync();
        try
        {
            await LoadDataCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            await EndLoadAsync();
        }
    }

    private async Task LoadDataCoreAsync()
    {
        await YieldToBackgroundAsync();

        var pageSize = PageSize;
        var currentPage = CurrentPage;
        var usesPaging = UsesPaging;

        await using var scope = App.Current.Services.CreateAsyncScope();
        _loadServiceProvider = scope.ServiceProvider;
        try
        {
            var crudService = CreateScopedCrudService(scope.ServiceProvider);
            var query = crudService.Query();
            query = ApplyQuery(query);
            query = ApplySearch(query);
            var totalItems = await query.CountAsync().ConfigureAwait(false);

            int totalPages;
            var resolvedPage = currentPage;
            if (usesPaging)
            {
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                if (resolvedPage >= totalPages && totalPages > 0)
                    resolvedPage = totalPages - 1;
                if (totalPages <= 0 || resolvedPage < 0)
                    resolvedPage = 0;
            }
            else
            {
                totalPages = totalItems > 0 ? 1 : 0;
                resolvedPage = 0;
            }

            query = ApplySorting(query);
            if (usesPaging)
                query = query.Page(resolvedPage, pageSize);
            var rows = await LoadPageRowsAsync(query, scope.ServiceProvider).ConfigureAwait(false);
            var entities = rows.Select(row => row.Entity).ToList();
            var loadedCount = rows.Count;

            await DispatcherUiHelper.InvokeOnUiAsync(() =>
            {
                TotalItems = totalItems;
                _suppressPageLoad = true;
                TotalPages = totalPages;
                CurrentPage = resolvedPage;
                _suppressPageLoad = false;
                Items = new ObservableCollection<TRowView>(rows);
                OnPropertyChanged(nameof(ItemsInfo));
                OnItemsLoaded(entities);
                LogListRead(loadedCount);
            });
        }
        finally
        {
            _loadServiceProvider = null;
        }
    }

    private void LogListRead(int loadedCount)
    {
        var criteria = BuildListReadCriteria();
        App.Current.Services.GetRequiredService<IAppLog>().Info(
            EntityLogFormatter.FormatListRead(typeof(TEntity), TotalItems, loadedCount, criteria));
    }

    private string BuildListReadCriteria()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(SearchText))
            parts.Add($"search=\"{SearchText.Trim()}\"");
        if (!string.IsNullOrWhiteSpace(SortColumn))
            parts.Add($"sort={SortColumn} {SortDirection}");
        if (UsesPaging && TotalPages > 0)
            parts.Add($"page={CurrentPage + 1}/{TotalPages}");
        return parts.Count == 0 ? string.Empty : $"({string.Join(", ", parts)})";
    }

    protected virtual void OnItemsLoaded(IReadOnlyList<TEntity> items)
    {
    }

    [RelayCommand]
    protected virtual void CreateItem()
    {
        ShowAddEditDialog();
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    protected virtual void EditItem()
    {
        if (SelectedItems.Count != 1) return;
        var id = GetEntityId(SelectedItems[0].Entity);
        ShowAddEditDialog(id);
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    protected virtual async Task DeleteItem()
    {
        if (SelectedItems.Count == 0) return;
        try
        {
            var keys = SelectedItems
                .Select(row => GetEntityId(row.Entity))
                .Distinct()
                .ToList();
            var ids = SelectedItems
                .Select(row => GetEntityIntId(row.Entity))
                .Distinct()
                .ToList();
            if (keys.Count == 0 || ids.Count == 0)
                return;
            var deleteConfirmation = App.Current.Services.GetRequiredService<IConfirmDialogService>();
            if (!await deleteConfirmation.ConfirmDeleteIfOfficialResultsAffectedAsync<TEntity>(ids))
                return;
            foreach (var key in keys)
                await CrudService.DeleteAsync(key);
            await LoadDataAsync();
        }
        catch (Exception)
        {
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
    private void GoToFirstPage()
    {
        CurrentPage = 0;
        LoadDataCommand.Execute(null);
    }

    private bool CanGoToFirstPage() => CurrentPage > 0;

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void GoToPreviousPage()
    {
        if (CurrentPage <= 0) return;
        CurrentPage--;
        LoadDataCommand.Execute(null);
    }

    private bool CanGoToPreviousPage() => CurrentPage > 0;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void GoToNextPage()
    {
        if (CurrentPage >= TotalPages - 1) return;
        CurrentPage++;
        LoadDataCommand.Execute(null);
    }

    private bool CanGoToNextPage() => TotalPages > 0 && CurrentPage < TotalPages - 1;

    [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
    private void GoToLastPage()
    {
        CurrentPage = TotalPages - 1;
        LoadDataCommand.Execute(null);
    }

    private bool CanGoToLastPage() => TotalPages > 0 && CurrentPage < TotalPages - 1;
    protected virtual bool CanEdit()
    {
        return SelectedItems.Count == 1;
    }

    protected virtual bool CanDelete()
    {
        return SelectedItems.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanOpenDetails))]
    private void OpenDetails()
    {
        if (SelectedItems.Count != 1)
            return;
        var entity = SelectedItems[0].Entity;
        var id = GetEntityId(entity);
        if (id == null)
            return;
        var entityName = typeof(TEntity).Name;
        if (!int.TryParse(id.ToString(), out var entityId))
            return;
        switch (entityName)
        {
            case nameof(Athlete):
                _navigationService.NavigateTo<AthleteDetailsViewModel>(NavigationContext.ForId(entityId));
                break;
            case nameof(Club):
                _navigationService.NavigateTo<ClubDetailsViewModel>(NavigationContext.ForId(entityId));
                break;
            case nameof(Entry):
                if (entity is Entry entry)
                    OpenEntryDetails(entry);
                break;
            case nameof(SwimEvent):
                _navigationService.NavigateTo<EventDetailsViewModel>(NavigationContext.ForId(entityId));
                break;
            case nameof(AgeGroup):
                _navigationService.NavigateTo<AgeGroupDetailsViewModel>(NavigationContext.ForId(entityId));
                break;
            case nameof(SwimStyle):
                _navigationService.NavigateTo<SwimStyleDetailsViewModel>(NavigationContext.ForId(entityId));
                break;
        }
    }

    protected virtual void OpenEntryDetails(Entry entry) =>
        NavigationService.NavigateTo<EntryDetailsViewModel>(NavigationContext.ForId(entry.Id));

    private bool CanOpenDetails()
    {
        return SelectedItems.Count == 1;
    }

    [RelayCommand]
    private void SortByColumn(string? column)
    {
        if (string.IsNullOrEmpty(column)) return;
        if (string.Equals(SortColumn, column, StringComparison.Ordinal))
        {
            SortDirection = SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            SortColumn = column;
            SortDirection = ListSortDirection.Ascending;
        }
        if (CurrentPage != 0)
            CurrentPage = 0;
        else
            LoadDataCommand.Execute(null);
    }

    protected virtual IQueryable<TEntity> ApplyQuery(IQueryable<TEntity> query)
    {
        return query;
    }

    protected virtual IQueryable<TEntity> ApplySearch(IQueryable<TEntity> query)
    {
        return query;
    }

    protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query)
    {
        if (string.IsNullOrEmpty(SortColumn))
            return query;
        foreach (var col in ColumnConfigurations)
        {
            if (col is not ColumnConfiguration<TEntity> typed)
                continue;
            var sortKey = typed.GetSortKey();
            if (!string.Equals(sortKey, SortColumn, StringComparison.Ordinal))
                continue;
            return typed.SortQuery(query, SortDirection);
        }
        try
        {
            return ColumnConfiguration<TEntity>.SortQueryableByPropertyPath(query, SortColumn, SortDirection);
        }
        catch (InvalidOperationException)
        {
            return query;
        }
    }

    protected virtual void ShowAddEditDialog(TKey? id = default)
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        if (factory.ShowGenericAddEdit(id, CrudService) == true)
            _ = LoadDataAsync();
    }

    protected virtual TKey GetEntityId(TEntity entity)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} does not have an Id property");
        return (TKey)idProperty.GetValue(entity)!;
    }

    protected virtual int GetEntityIntId(TEntity entity)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} does not have an Id property");
        var value = idProperty.GetValue(entity);
        if (value is int id)
            return id;
        throw new InvalidOperationException($"Entity {typeof(TEntity).Name} Id property is not an int");
    }
}
