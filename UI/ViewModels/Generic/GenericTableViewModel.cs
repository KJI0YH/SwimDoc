using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.QueryObjects;
using ServiceLayer.Crud;
using UI.Views;
using UI.Views.Generic;
using Expression = System.Linq.Expressions.Expression;

namespace UI.ViewModels.Generic;

public partial class GenericTableViewModel<TEntity, TKey> : GenericTableViewModelBase
    where TEntity : class
{
    protected readonly ICrudService<TEntity, TKey> _crudService;

    public override ObservableCollection<ColumnConfiguration> GetColumnConfigurations() => ColumnConfigurations;

    public override bool GetAutoGenerateColumns() => AutoGenerateColumns;

    [ObservableProperty] private ObservableCollection<TEntity> _items = new();

    [ObservableProperty] private TEntity? _selectedItem;

    [ObservableProperty] private int _currentPage = 0;

    [ObservableProperty] private int _pageSize = 20;

    [ObservableProperty] private int _totalItems;

    [ObservableProperty] private int _totalPages;

    [ObservableProperty] private string _sortColumn = string.Empty;

    [ObservableProperty] private ListSortDirection _sortDirection = ListSortDirection.Ascending;

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private ObservableCollection<ColumnConfiguration> _columnConfigurations = new();

    [ObservableProperty] private bool _autoGenerateColumns = true;

    public GenericTableViewModel(ICrudService<TEntity, TKey> crudService)
    {
        _crudService = crudService;
        InitializeColumns();
        LoadDataCommand.Execute(null);
    }

    protected virtual void InitializeColumns()
    {
        AutoGenerateColumns = true;
    }

    partial void OnSelectedItemChanged(TEntity? value)
    {
        EditItemCommand.NotifyCanExecuteChanged();
        DeleteItemCommand.NotifyCanExecuteChanged();
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 0;
        LoadDataCommand.Execute(null);
    }

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 0;
        LoadDataCommand.Execute(null);
    }

    public string PageInfo => $"Страница {CurrentPage + 1} из {TotalPages} (Всего: {TotalItems})";

    [RelayCommand]
    protected async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var query = _crudService.Query();
            query = ApplyQuery(query);
            query = ApplySearch(query);
            query = ApplySorting(query);

            TotalItems = query.Count();
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

            if (CurrentPage >= TotalPages && TotalPages > 0)
                CurrentPage = TotalPages - 1;

            var pagedQuery = query.Page(CurrentPage, PageSize);
            var items = pagedQuery.ToList();

            Items = new ObservableCollection<TEntity>(items);
            OnPropertyChanged(nameof(PageInfo));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CreateItem()
    {
        ShowAddEditDialog();
    }

    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void EditItem()
    {
        if (SelectedItem == null) return;
        var id = GetEntityId(SelectedItem);
        ShowAddEditDialog(id);
    }

    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private async Task DeleteItem()
    {
        if (SelectedItem == null) return;

        try
        {
            var id = GetEntityId(SelectedItem);
            await _crudService.DeleteAsync(id);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            // ignored
        }
    }
    
    private bool CanEditOrDelete() => SelectedItem != null;

    [RelayCommand]
    private void SortByColumn(string? column)
    {
        if (string.IsNullOrEmpty(column)) return;

        if (SortColumn == column)
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

        CurrentPage = 0;
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

        try
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var property = Expression.Property((Expression)parameter, (string)SortColumn);
            var lambda = Expression.Lambda(property, parameter);

            var methodName = SortDirection == ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";
            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(TEntity), property.Type],
                query.Expression,
                Expression.Quote(lambda));

            return query.Provider.CreateQuery<TEntity>(resultExpression);
        }
        catch
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
            if (window.DialogResult == true)
            {
                await LoadDataAsync();
            }
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