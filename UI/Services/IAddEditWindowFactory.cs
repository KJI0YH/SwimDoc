using System.Windows;

namespace UI.Services;

public interface IAddEditWindowFactory
{
    bool? CreateAndShow<TWindow>(int? id = null) where TWindow : Window;
    bool? CreateAndShow<TWindow>(int? id, AddEditContext? context) where TWindow : Window;

    TWindow CreateAndShowAndReturn<TWindow>(int? id = null) where TWindow : Window;
    TWindow CreateAndShowAndReturn<TWindow>(int? id, AddEditContext? context) where TWindow : Window;
}