using System.Windows;
using System.Windows.Controls;
using UI.Services.Navigation;

namespace UI.Helpers.Navigation;

public static class DataPageNavigation
{
    public static void WireLoaded(Page page)
    {
        page.Loaded += OnPageLoaded;
    }

    private static void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IDataLoadable loadable })
            loadable.EnsureDataLoaded();
    }
}
