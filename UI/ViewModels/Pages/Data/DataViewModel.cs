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
using DataLayer.QueryObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Crud;
using UI.Services;
using UI.Views.Windows;
using UI.Views.Windows.AddEdit;

namespace UI.ViewModels.Pages.Data;

public partial class DataViewModel<TEntity, TKey> : DataViewModelBase
    where TEntity : class
{
    protected readonly ICrudService<TEntity, TKey> _crudService;
    private readonly INavigationService _navigationService;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private bool _suppressPageLoad;

    [ObservableProperty] private bool _autoGenerateColumns = true;

    [ObservableProperty] private ObservableCollection<ColumnConfiguration> _columnConfigurations = new();

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private ObservableCollection<TEntity> _items = new();

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private TEntity? _selectedItem;

    [ObservableProperty] private ObservableCollection<TEntity> _selectedItems = new();

    [ObservableProperty] private string _sortColumn = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsInfo))]
    private int _currentPage = 0;

    [ObservableProperty] private ObservableCollection<int> _pageOptions = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsInfo))]
    private int _pageSize = 20;

    public string ItemsInfo
    {
        get
        {
            if (TotalItems <= 0) return $"0-0 из {TotalItems}";
            var from = CurrentPage * PageSize + 1;
            var to = Math.Min(TotalItems, CurrentPage * PageSize + PageSize);
            return $"{from}-{to} из {TotalItems}";
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

    public DataViewModel(ICrudService<TEntity, TKey> crudService)
    {
        _crudService = crudService;
        _navigationService = App.Current.Services.GetRequiredService<INavigationService>();
        InitializeColumns();
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

    partial void OnSelectedItemsChanged(ObservableCollection<TEntity> value)
    {
        EditItemCommand.NotifyCanExecuteChanged();
        DeleteItemCommand.NotifyCanExecuteChanged();
        OpenDetailsCommand.NotifyCanExecuteChanged();
    }

    public override void SyncSelectedItemsFromGrid(IList? gridSelection)
    {
        var list = new List<TEntity>();
        if (gridSelection != null)
            foreach (var item in gridSelection)
                if (item is TEntity e)
                    list.Add(e);

        SelectedItems = new ObservableCollection<TEntity>(list);
        SelectedItem = list.Count == 1 ? list[0] : null;
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
    protected async Task LoadDataAsync()
    {
        await _loadGate.WaitAsync();
        IsLoading = true;
        try
        {
            var query = _crudService.Query();
            query = ApplyQuery(query);
            query = ApplySearch(query);
            query = ApplySorting(query);

            TotalItems = await query.CountAsync();

            _suppressPageLoad = true;
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

            if (CurrentPage >= TotalPages && TotalPages > 0)
                CurrentPage = TotalPages - 1;
            if (TotalPages <= 0 || CurrentPage < 0)
                CurrentPage = 0;
            _suppressPageLoad = false;

            query = query.Page(CurrentPage, PageSize);
            var items = await query.ToListAsync();
            Items = new ObservableCollection<TEntity>(items);
            OnPropertyChanged(nameof(ItemsInfo));
            OnItemsLoaded(items);
        }
        finally
        {
            IsLoading = false;
            _loadGate.Release();
        }
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
        var id = GetEntityId(SelectedItems[0]);
        ShowAddEditDialog(id);
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    protected virtual async Task DeleteItem()
    {
        if (SelectedItems.Count == 0) return;

        try
        {
            var ids = SelectedItems.Select(GetEntityId).ToList();
            foreach (var id in ids)
                await _crudService.DeleteAsync(id);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            // ignored
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

        var id = GetEntityId(SelectedItems[0]);
        if (id == null)
            return;

        var entityName = typeof(TEntity).Name;
        if (!int.TryParse(id.ToString(), out var entityId))
            return;

        switch (entityName)
        {
            case nameof(Athlete):
                _navigationService.NavigateTo<AthleteDetailsViewModel>(entityId);
                break;
            case nameof(Club):
                _navigationService.NavigateTo<ClubDetailsViewModel>(entityId);
                break;
            case nameof(Entry):
                _navigationService.NavigateTo<EntryDetailsViewModel>(entityId);
                break;
            case nameof(SwimEvent):
                _navigationService.NavigateTo<EventDetailsViewModel>(entityId);
                break;
            case nameof(AgeGroup):
                _navigationService.NavigateTo<AgeGroupDetailsViewModel>(entityId);
                break;
            case nameof(SwimStyle):
                _navigationService.NavigateTo<SwimStyleDetailsViewModel>(entityId);
                break;
        }
    }

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
        var window = new GenericAddEditWindow(id, _crudService);
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ??
                    Application.Current.MainWindow;
        if (owner != null)
        {
            window.Owner = owner;
            if (owner is MainWindow mainWindow)
            {
                mainWindow.ShowModalOverlay();
                mainWindow.Dispatcher.Invoke(DispatcherPriority.Render, static () => { });
            }
            else
            {
                var originalOpacity = owner.Opacity;
                owner.Opacity = 0.75;
                owner.Dispatcher.Invoke(DispatcherPriority.Render, static () => { });
                window.Closed += (_, _) => owner.Opacity = originalOpacity;
            }
        }

        window.Closed += async (s, e) =>
        {
            if (window.DialogResult == true) await LoadDataAsync();
        };
        try
        {
            window.ShowDialog();
        }
        finally
        {
            if (owner is MainWindow mw)
                mw.HideModalOverlay();
        }
    }

    protected virtual TKey GetEntityId(TEntity entity)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} does not have an Id property");

        return (TKey)idProperty.GetValue(entity)!;
    }
}