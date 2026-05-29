using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLayer.Crud;
using UI.Resources;
using UI.Services;

namespace UI.ViewModels.Windows.AddEdit;

public partial class AddEditViewModel<TEntity, TKey> : ViewModelBase, IWindowResult
    where TEntity : class
{
    private readonly TKey? _id;
    protected readonly ICrudService<TEntity, TKey> CrudService;
    protected readonly bool IsAdd;
    protected readonly bool IsEdit;

    [ObservableProperty] private ObservableCollection<string> _validationErrors = new();

    public AddEditViewModel(TKey? id, ICrudService<TEntity, TKey> crudService)
    {
        _id = id;
        IsAdd = _id == null || _id is 0;
        IsEdit = !IsAdd;
        CrudService = crudService;
        ValidationErrors.CollectionChanged += OnValidationErrorsCollectionChanged;
    }

    public bool HasErrors => ValidationErrors.Count > 0;

    protected TEntity? Entity { get; set; }

    public virtual string WindowTitle => IsAdd ? Strings.WindowMode_Create : Strings.WindowMode_Edit;

    object? IWindowResult.Result => Entity;

    private void OnValidationErrorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasErrors));
    }

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
            foreach (var error in errors) ValidationErrors.Add(error.ErrorMessage ?? Strings.Validation_ErrorFallback);
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