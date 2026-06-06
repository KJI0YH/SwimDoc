namespace UI.Services;

public interface IAddEditWindowFactory
{
    bool? CreateAndShow<TDialog>(int? id = null, NavigationContext? context = null) where TDialog : class;

    AddEditDialogResult CreateAndShowAndReturn<TDialog>(int? id = null, NavigationContext? context = null)
        where TDialog : class;

    bool? ShowGenericAddEdit(object? id, object crudService);
}
