using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Crud;
using UI.ViewModels.Generic;
using Expression = System.Linq.Expressions.Expression;

namespace UI.Views.Generic;

public partial class GenericAddEditWindow : Window
{
    private readonly object _viewModel;

    public GenericAddEditWindow(object? id, object crudService)
    {
        if (crudService == null)
            throw new ArgumentNullException(nameof(crudService));

        // Извлекаем типы TEntity и TKey из интерфейса ICrudService<TEntity, TKey>
        var crudServiceType = crudService.GetType();
        var interfaceType = crudServiceType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICrudService<,>));

        if (interfaceType == null)
            throw new InvalidOperationException($"The provided service does not implement ICrudService<TEntity, TKey>.");

        var genericArgs = interfaceType.GetGenericArguments();
        var entityType = genericArgs[0];
        var keyType = genericArgs[1];

        // Преобразуем id в нужный тип, если он не null
        object? typedId = null;
        if (id != null)
        {
            if (keyType.IsInstanceOfType(id))
            {
                typedId = id;
            }
            else
            {
                typedId = Convert.ChangeType(id, keyType);
            }
        }

        // Создаем или загружаем Entity
        object entity;
        bool isNew = typedId == null;

        if (isNew)
        {
            // Создаем новый пустой Entity через рефлексию
            entity = Activator.CreateInstance(entityType)
                ?? throw new InvalidOperationException($"Failed to create instance of {entityType.Name}.");
        }
        else
        {
            // Загружаем Entity через crudService по id
            entity = LoadEntitySync(crudService, crudServiceType, entityType, keyType, typedId!)
                ?? throw new InvalidOperationException($"Entity with Id {typedId} was not found.");
        }

        // Создаем generic тип ViewModel
        var viewModelType = typeof(GenericAddEditViewModel<,>).MakeGenericType(entityType, keyType);
        
        // Ищем конструктор, который принимает Entity, bool isNew, и ICrudService
        var crudServiceInterfaceType = typeof(ICrudService<,>).MakeGenericType(entityType, keyType);
        var constructor = viewModelType.GetConstructor(new[] { entityType, typeof(bool), crudServiceInterfaceType });
        
        if (constructor == null)
            throw new InvalidOperationException($"Could not find constructor for GenericAddEditViewModel<{entityType.Name}, {keyType.Name}> with parameters (TEntity, bool, ICrudService).");

        // Создаем ViewModel с уже готовой Entity
        _viewModel = constructor.Invoke(new[] { entity, isNew, crudService }) 
            ?? throw new InvalidOperationException("Failed to create ViewModel instance.");
        
        // Подписываемся на событие закрытия
        var closeRequestedEvent = viewModelType.GetEvent("CloseRequested");
        if (closeRequestedEvent != null)
        {
            var handler = new EventHandler(OnCloseRequested);
            closeRequestedEvent.AddEventHandler(_viewModel, handler);
        }
        
        InitializeComponent();
        DataContext = _viewModel;

        // Инициализируем ViewModel асинхронно после загрузки окна
        Loaded += async (s, e) =>
        {
            var initializeMethod = viewModelType.GetMethod("InitializeAsync", BindingFlags.Public | BindingFlags.Instance);
            if (initializeMethod != null)
            {
                var task = initializeMethod.Invoke(_viewModel, null) as Task;
                if (task != null)
                {
                    await task;
                }
            }
        };
    }

    private object? LoadEntitySync(object crudService, Type crudServiceType, Type entityType, Type keyType, object id)
    {
        // Получаем метод Query из ICrudService
        var queryMethod = crudServiceType.GetMethod("Query", new[] { typeof(bool) });
        if (queryMethod == null)
        {
            queryMethod = crudServiceType.GetMethod("Query", Type.EmptyTypes);
        }

        if (queryMethod == null)
            throw new InvalidOperationException("Could not find Query method in CrudService.");

        // Вызываем Query(asNoTracking: false)
        var queryable = queryMethod.Invoke(crudService, queryMethod.GetParameters().Length > 0 ? new object[] { false } : null);
        if (queryable == null)
            throw new InvalidOperationException("Query method returned null.");

        // Создаем выражение для фильтрации по Id
        var idProperty = entityType.GetProperty("Id");
        if (idProperty == null)
            throw new InvalidOperationException($"Entity type {entityType.Name} does not have an Id property.");

        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, idProperty);
        var constant = Expression.Constant(id, keyType);
        var equals = Expression.Equal(property, constant);
        var lambdaType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
        var lambda = Expression.Lambda(lambdaType, equals, parameter);

        // Вызываем Where через рефлексию, затем ToList и FirstOrDefault
        var whereMethod = typeof(Queryable).GetMethods()
            .FirstOrDefault(m => m.Name == "Where" && m.GetParameters().Length == 2)?
            .MakeGenericMethod(entityType);

        if (whereMethod == null)
            throw new InvalidOperationException("Could not find Where method.");

        var filteredQuery = whereMethod.Invoke(null, new[] { queryable, lambda });
        if (filteredQuery == null)
            throw new InvalidOperationException("Where method returned null.");

        // Преобразуем в список и берем первый элемент
        var toListMethod = typeof(Enumerable).GetMethod("ToList")?.MakeGenericMethod(entityType);
        if (toListMethod == null)
            throw new InvalidOperationException("Could not find ToList method.");

        var list = toListMethod.Invoke(null, new[] { filteredQuery });
        if (list == null)
            return null;

        // Получаем первый элемент из списка
        var firstOrDefaultMethod = typeof(Enumerable).GetMethod("FirstOrDefault")?.MakeGenericMethod(entityType);
        if (firstOrDefaultMethod == null)
            throw new InvalidOperationException("Could not find FirstOrDefault method.");

        return firstOrDefaultMethod.Invoke(null, new[] { list });
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }
}

