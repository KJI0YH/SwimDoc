using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.QueryObjects;
using ServiceLayer.Crud;
using UI.Views;

namespace UI.ViewModels;

public partial class GenericTableViewModel<TEntity, TKey> : GenericTableViewModelBase
    where TEntity : class
{
    private readonly ICrudService<TEntity, TKey> _crudService;

    public override ObservableCollection<ColumnConfiguration> GetColumnConfigurations() => ColumnConfigurations;
    
    public override bool GetAutoGenerateColumns() => AutoGenerateColumns;

    [ObservableProperty]
    private ObservableCollection<TEntity> _items = new();

    [ObservableProperty]
    private TEntity? _selectedItem;

    [ObservableProperty]
    private int _currentPage = 0;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private string _sortColumn = string.Empty;

    [ObservableProperty]
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<ColumnConfiguration> _columnConfigurations = new();

    [ObservableProperty]
    private bool _autoGenerateColumns = true;

    public GenericTableViewModel(ICrudService<TEntity, TKey> crudService)
    {
        _crudService = crudService;
        InitializeColumns();
        LoadDataCommand.Execute(null);
    }

    protected virtual void InitializeColumns()
    {
        // По умолчанию используем автоматическую генерацию колонок
        // Переопределите этот метод в наследниках для настройки колонок
        AutoGenerateColumns = true;
    }

    partial void OnSelectedItemChanged(TEntity? value)
    {
        EditItemCommand.NotifyCanExecuteChanged();
        DeleteItemCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentPageChanged(int value)
    {
        GoToFirstPageCommand.NotifyCanExecuteChanged();
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
        GoToLastPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 0;
        LoadDataCommand.Execute(null);
    }

    partial void OnTotalPagesChanged(int value)
    {
        GoToNextPageCommand.NotifyCanExecuteChanged();
        GoToLastPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 0;
        LoadDataCommand.Execute(null);
    }

    public string PageInfo => $"Страница {CurrentPage + 1} из {TotalPages} (Всего: {TotalItems})";

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var query = _crudService.Query();
            query = ApplySearch(query);
            query = ApplySorting(query);

            TotalItems = await Task.Run(() => query.Count());
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

            if (CurrentPage >= TotalPages && TotalPages > 0)
                CurrentPage = TotalPages - 1;

            var pagedQuery = query.Page(CurrentPage, PageSize);
            var items = await Task.Run(() => pagedQuery.ToList());

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
        var newItem = Activator.CreateInstance<TEntity>();
        ShowEditDialog(newItem, isNew: true);
    }

    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void EditItem()
    {
        if (SelectedItem == null) return;
        ShowEditDialog(SelectedItem, isNew: false);
    }

    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private async Task DeleteItem()
    {
        if (SelectedItem == null) return;

        var result = System.Windows.MessageBox.Show(
            "Вы уверены, что хотите удалить выбранный элемент?",
            "Подтверждение удаления",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                var id = GetEntityId(SelectedItem);
                await _crudService.DeleteAsync(id);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Ошибка при удалении: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private bool CanEditOrDelete() => SelectedItem != null;

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
        if (CurrentPage > 0)
        {
            CurrentPage--;
            LoadDataCommand.Execute(null);
        }
    }

    private bool CanGoToPreviousPage() => CurrentPage > 0;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void GoToNextPage()
    {
        if (CurrentPage < TotalPages - 1)
        {
            CurrentPage++;
            LoadDataCommand.Execute(null);
        }
    }

    private bool CanGoToNextPage() => CurrentPage < TotalPages - 1;

    [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
    private void GoToLastPage()
    {
        CurrentPage = TotalPages - 1;
        LoadDataCommand.Execute(null);
    }

    private bool CanGoToLastPage() => CurrentPage < TotalPages - 1;

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
            var property = Expression.Property(parameter, SortColumn);
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


    protected virtual List<FieldConfiguration> GetFieldConfigurations()
    {
        // По умолчанию возвращаем null, что означает автоматическую генерацию всех полей
        return new List<FieldConfiguration>();
    }

    protected virtual List<CustomValidationRule> GetCustomValidationRules()
    {
        // По умолчанию нет кастомных правил валидации
        return new List<CustomValidationRule>();
    }

    protected virtual void ShowEditDialog(TEntity item, bool isNew)
    {
        var fieldConfigurations = GetFieldConfigurations();
        var validationRules = GetCustomValidationRules();
        var window = GenericEditWindowFactory.Create(item, isNew, _crudService, fieldConfigurations, validationRules);
        window.Closed += async (s, e) =>
        {
            if (window.DialogResult == true)
            {
                await LoadDataAsync();
            }
        };
        window.ShowDialog();
    }

    protected virtual TKey GetEntityId(TEntity entity)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} does not have an Id property");

        return (TKey)idProperty.GetValue(entity)!;
    }

}

