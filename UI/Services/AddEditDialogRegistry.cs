using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.Crud;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.SwimStyleService;
using UI.ViewModels.Dialogs.AddEdit;
using UI.ViewModels.Dialogs.CombinedResultsReportGeneration;
using UI.ViewModels.Dialogs.HeatAllocationParameters;
using UI.ViewModels.Dialogs.LoadEntriesFromPreviousEvent;
using UI.ViewModels.Dialogs.ReportGeneration;
using UI.ViewModels.Dialogs.StartTimeCalculation;
using UI.Views.Dialogs;
using UI.Views.Dialogs.AddEdit;
using UI.Views.Dialogs.Markers.AddEdit;
using UI.Views.Dialogs.Markers.CombinedResultsReportGeneration;
using UI.Views.Dialogs.Markers.HeatAllocationParameters;
using UI.Views.Dialogs.Markers.LoadEntriesFromPreviousEvent;
using UI.Views.Dialogs.Markers.ReportGeneration;
using UI.Views.Dialogs.Markers.StartTimeCalculation;
using Expression = System.Linq.Expressions.Expression;

namespace UI.Services;

internal sealed class AddEditDialogRegistry
{
    private readonly Dictionary<Type, DialogDefinition> _definitions = new();

    public AddEditDialogRegistry()
    {
        Register<int?>(typeof(AgeGroupAddEditWindow), CreateAgeGroupViewModel, () => new AgeGroupAddEditView());
        Register<int?>(typeof(AthleteAddEditWindow), CreateAthleteViewModel, () => new AthleteAddEditView());
        Register<int?>(typeof(ClubAddEditWindow), CreateClubViewModel, () => new ClubAddEditView());
        Register<int?>(typeof(EntryAddEditWindow), CreateEntryViewModel, () => new EntryAddEditView());
        Register<int?>(typeof(EventAddEditWindow), CreateEventViewModel, () => new EventAddEditView());
        Register<int?>(typeof(HeatAddEditWindow), CreateHeatViewModel, () => new HeatAddEditView());
        Register<int?>(typeof(SwimStyleAddEditWindow), CreateSwimStyleViewModel, () => new SwimStyleAddEditView());
        Register<int?>(typeof(HeatAllocationParametersWindow), _ => new HeatAllocationParametersViewModel(), () => new HeatAllocationParametersView());
        Register<int?>(typeof(ReportGenerationWindow), _ => new ReportGenerationViewModel(), () => new ReportGenerationView());
        Register<int?>(typeof(StartTimeCalculationWindow), _ =>
        {
            var eventService = App.Current.Services.GetRequiredService<IEventService>();
            return new StartTimeCalculationViewModel(eventService);
        }, () => new StartTimeCalculationView());
        Register<int?>(typeof(LoadEntriesFromPreviousEventWindow), _ =>
        {
            var eventService = App.Current.Services.GetRequiredService<IEventService>();
            return new LoadEntriesFromPreviousEventViewModel(eventService);
        }, () => new LoadEntriesFromPreviousEventView());
        Register<int?>(typeof(CombinedResultsReportGenerationWindow), _ => new CombinedResultsReportGenerationViewModel(), () => new CombinedResultsReportGenerationView());
    }

    public bool TryGet(Type dialogType, out DialogDefinition definition) =>
        _definitions.TryGetValue(dialogType, out definition!);

    public object CreateGenericViewModel(object? id, object crudService) =>
        CreateGenericViewModelCore(id, crudService);

    public FrameworkElement CreateGenericView() => new GenericAddEditView();

    private void Register<TId>(Type dialogType, Func<TId, object> createViewModel, Func<FrameworkElement> createView)
    {
        _definitions[dialogType] = new DialogDefinition(
            id => createViewModel((TId)id!),
            createView);
    }

    private static object CreateAgeGroupViewModel(int? id)
    {
        var service = App.Current.Services.GetRequiredService<IAgeGroupService>();
        return new AgeGroupAddViewModel(id, service);
    }

    private static object CreateAthleteViewModel(int? id)
    {
        var athleteService = App.Current.Services.GetRequiredService<IAthleteService>();
        var clubService = App.Current.Services.GetRequiredService<IClubService>();
        return new AthleteAddViewModel(id, athleteService, clubService);
    }

    private static object CreateClubViewModel(int? id)
    {
        var service = App.Current.Services.GetRequiredService<IClubService>();
        return new ClubAddViewModel(id, service);
    }

    private static object CreateEntryViewModel(int? id)
    {
        var entryService = App.Current.Services.GetRequiredService<IEntryService>();
        var athleteService = App.Current.Services.GetRequiredService<IAthleteService>();
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        var clubService = App.Current.Services.GetRequiredService<IClubService>();
        return new EntryViewModel(id, entryService, athleteService, clubService, eventService);
    }

    private static object CreateEventViewModel(int? id)
    {
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        var ageGroupService = App.Current.Services.GetRequiredService<IAgeGroupService>();
        var swimStyleService = App.Current.Services.GetRequiredService<ISwimStyleService>();
        return new EventAddViewModel(id, eventService, ageGroupService, swimStyleService);
    }

    private static object CreateHeatViewModel(int? id)
    {
        var heatService = App.Current.Services.GetRequiredService<IHeatService>();
        var entryService = App.Current.Services.GetRequiredService<IEntryService>();
        var eventService = App.Current.Services.GetRequiredService<IEventService>();
        return new HeatAddEditViewModel(id, heatService, entryService, eventService);
    }

    private static object CreateSwimStyleViewModel(int? id)
    {
        var service = App.Current.Services.GetRequiredService<ISwimStyleService>();
        return new SwimStyleAddViewModel(id, service);
    }

    private static object CreateGenericViewModelCore(object? id, object crudService)
    {
        if (crudService == null)
            throw new ArgumentNullException(nameof(crudService));

        var crudServiceType = crudService.GetType();
        var interfaceType = crudServiceType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICrudService<,>));

        if (interfaceType == null)
            throw new InvalidOperationException("The provided service does not implement ICrudService<TEntity, TKey>.");

        var genericArgs = interfaceType.GetGenericArguments();
        var entityType = genericArgs[0];
        var keyType = genericArgs[1];

        object? typedId = null;
        if (id != null)
            typedId = keyType.IsInstanceOfType(id) ? id : Convert.ChangeType(id, keyType);

        object entity;
        var isNew = typedId == null;

        if (isNew)
            entity = Activator.CreateInstance(entityType)
                     ?? throw new InvalidOperationException($"Failed to create instance of {entityType.Name}.");
        else
            entity = LoadEntitySync(crudService, crudServiceType, entityType, keyType, typedId!)
                     ?? throw new InvalidOperationException($"Entity with Id {typedId} was not found.");

        var viewModelType = typeof(AddEditViewModel<,>).MakeGenericType(entityType, keyType);
        var crudServiceInterfaceType = typeof(ICrudService<,>).MakeGenericType(entityType, keyType);
        var constructor = viewModelType.GetConstructor([entityType, typeof(bool), crudServiceInterfaceType])
                          ?? throw new InvalidOperationException(
                              $"Could not find constructor for AddEditViewModel<{entityType.Name}, {keyType.Name}>.");

        return constructor.Invoke([entity, isNew, crudService])
               ?? throw new InvalidOperationException("Failed to create ViewModel instance.");
    }

    private static object? LoadEntitySync(object crudService, Type crudServiceType, Type entityType, Type keyType, object id)
    {
        var queryMethod = crudServiceType.GetMethod("Query", [typeof(bool)])
                          ?? crudServiceType.GetMethod("Query", Type.EmptyTypes);

        if (queryMethod == null)
            throw new InvalidOperationException("Could not find Query method in CrudService.");

        var queryable = queryMethod.Invoke(crudService,
            queryMethod.GetParameters().Length > 0 ? [false] : null);
        if (queryable == null)
            throw new InvalidOperationException("Query method returned null.");

        var idProperty = entityType.GetProperty("Id")
                         ?? throw new InvalidOperationException($"Entity type {entityType.Name} does not have an Id property.");

        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, idProperty);
        var constant = Expression.Constant(id, keyType);
        var equals = Expression.Equal(property, constant);
        var lambdaType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
        var lambda = Expression.Lambda(lambdaType, equals, parameter);

        var whereMethod = typeof(Queryable).GetMethods()
            .FirstOrDefault(m => m.Name == "Where" && m.GetParameters().Length == 2)?
            .MakeGenericMethod(entityType)
            ?? throw new InvalidOperationException("Could not find Where method.");

        var filteredQuery = whereMethod.Invoke(null, [queryable, lambda]);
        if (filteredQuery == null)
            throw new InvalidOperationException("Where method returned null.");

        var toListMethod = typeof(Enumerable).GetMethod("ToList")?.MakeGenericMethod(entityType)
                           ?? throw new InvalidOperationException("Could not find ToList method.");

        var list = toListMethod.Invoke(null, [filteredQuery]);
        if (list == null)
            return null;

        var firstOrDefaultMethod = typeof(Enumerable).GetMethod("FirstOrDefault")?.MakeGenericMethod(entityType)
                                   ?? throw new InvalidOperationException("Could not find FirstOrDefault method.");

        return firstOrDefaultMethod.Invoke(null, [list]);
    }

    internal sealed class DialogDefinition(Func<object?, object> createViewModel, Func<FrameworkElement> createView)
    {
        public object CreateViewModel(object? id) => createViewModel(id);

        public FrameworkElement CreateView() => createView();
    }
}
