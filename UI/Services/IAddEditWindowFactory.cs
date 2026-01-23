using System.Windows;

namespace UI.Services;

public interface IAddEditWindowFactory
{
    bool? CreateAndShow<TWindow>(int? id = null) where TWindow : Window;

    /// <summary>
    /// Создаёт и показывает окно, возвращает экземпляр окна после закрытия.
    /// Позволяет получить сохранённую сущность через DataContext (IAddEditWindowResult).
    /// </summary>
    TWindow CreateAndShowAndReturn<TWindow>(int? id = null) where TWindow : Window;
}
