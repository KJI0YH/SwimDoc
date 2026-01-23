using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLayer.Crud;

namespace UI.ViewModels.Generic;

public partial class GenericAddEditViewModel<TEntity, TKey> : ViewModelBase, IAddEditWindowResult
    where TEntity : class
{
    private readonly TKey? _id;
    protected readonly ICrudService<TEntity, TKey> CrudService;
    protected readonly bool IsAdd;
    protected readonly bool IsEdit;

    [ObservableProperty] private ObservableCollection<string> _validationErrors = new();

    public bool HasErrors => ValidationErrors.Count > 0;

    public GenericAddEditViewModel(TKey? id, ICrudService<TEntity, TKey> crudService)
    {
        _id = id;
        IsAdd = _id == null || _id is 0;
        IsEdit = !IsAdd;
        CrudService = crudService;
        ValidationErrors.CollectionChanged += OnValidationErrorsCollectionChanged;
    }

    private void OnValidationErrorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasErrors));
    }

    protected TEntity? Entity { get; set; }

    object? IAddEditWindowResult.SavedEntity => Entity;

    public virtual string WindowTitle => IsAdd ? "Создание" : "Редактирование";

    protected virtual async Task<TEntity?> LoadEntityAsync(TKey? id)
    {
        if (id == null) return null;
        return await CrudService.FindAsync(id);
    }

    public virtual async Task InitializeAsync()
    {
        if (IsEdit)
        {
            var loadedEntity = await LoadEntityAsync(_id);
            Entity = loadedEntity ?? Activator.CreateInstance<TEntity>();
        }
        else
        {
            Entity = Activator.CreateInstance<TEntity>();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationErrors.Clear();
        ImmutableList<ValidationResult> errors;

        if (IsAdd)
        {
            var (entity, validationErrors) = await CrudService.CreateAsync(Entity);
            errors = validationErrors;
            Entity = entity;
        }
        else
        {
            var (entity, validationErrors) = await CrudService.UpdateAsync(Entity);
            errors = validationErrors;
            Entity = entity;
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors) ValidationErrors.Add(error.ErrorMessage ?? "Ошибка валидации");
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? CloseRequested;
}