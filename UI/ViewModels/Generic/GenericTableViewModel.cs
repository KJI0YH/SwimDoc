using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLayer.Crud;
using UI.Views;
using UI.Views.Generic;

namespace UI.ViewModels.Generic;

public partial class GenericTableViewModel<TEntity, TKey> : GenericTableViewModelBase
    where TEntity : class
{
    protected readonly ICrudService<TEntity, TKey> _crudService;

    public override ObservableCollection<ColumnConfiguration> GetColumnConfigurations() => ColumnConfigurations;

    public override bool GetAutoGenerateColumns() => AutoGenerateColumns;

    [ObservableProperty] private ObservableCollection<TEntity> _items = new();

    [ObservableProperty] private TEntity? _selectedItem;

    [ObservableProperty] private ObservableCollection<TEntity> _selectedItems = new();

    [ObservableProperty] private int _totalItems;

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

    partial void OnSelectedItemsChanged(ObservableCollection<TEntity> value)
    {
        EditItemCommand.NotifyCanExecuteChanged();
        DeleteItemCommand.NotifyCanExecuteChanged();
    }

    public override void SyncSelectedItemsFromGrid(IList? gridSelection)
    {
        var list = new List<TEntity>();
        if (gridSelection != null)
        {
            foreach (var item in gridSelection)
            {
                if (item is TEntity e)
                    list.Add(e);
            }
        }

        SelectedItems = new ObservableCollection<TEntity>(list);
        SelectedItem = list.Count == 1 ? list[0] : null;
    }

    partial void OnSearchTextChanged(string value)
    {
        LoadDataCommand.Execute(null);
    }

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

            var items = query.ToList();

            Items = new ObservableCollection<TEntity>(items);
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

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void EditItem()
    {
        if (SelectedItems.Count != 1) return;
        var id = GetEntityId(SelectedItems[0]);
        ShowAddEditDialog(id);
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteItem()
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

    private bool CanEdit() => SelectedItems.Count == 1;

    private bool CanDelete() => SelectedItems.Count > 0;

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

        return query;
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