using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace UI.Helpers.Controls;

public static class DataGridHeaderFitHelper
{
    private const double HeaderHorizontalPadding = 12;
    private const double SortGlyphExtra = 24;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridHeaderFitHelper),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty ColumnsChangedHandlerProperty =
        DependencyProperty.RegisterAttached(
            "ColumnsChangedHandler",
            typeof(NotifyCollectionChangedEventHandler),
            typeof(DataGridHeaderFitHelper),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
            return;
        if ((bool)e.NewValue)
            Attach(dataGrid);
        else
            Detach(dataGrid);
    }

    private static void Attach(DataGrid dataGrid)
    {
        if (dataGrid.GetValue(ColumnsChangedHandlerProperty) is not null)
            return;
        dataGrid.Loaded += OnDataGridLoaded;
        NotifyCollectionChangedEventHandler handler = (_, _) => ScheduleFit(dataGrid);
        dataGrid.SetValue(ColumnsChangedHandlerProperty, handler);
        dataGrid.Columns.CollectionChanged += handler;
        if (dataGrid.IsLoaded)
            ScheduleFit(dataGrid);
    }

    private static void Detach(DataGrid dataGrid)
    {
        dataGrid.Loaded -= OnDataGridLoaded;
        if (dataGrid.GetValue(ColumnsChangedHandlerProperty) is NotifyCollectionChangedEventHandler handler)
        {
            dataGrid.Columns.CollectionChanged -= handler;
            dataGrid.SetValue(ColumnsChangedHandlerProperty, null);
        }
    }

    private static void OnDataGridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
            ScheduleFit(dataGrid);
    }

    private static void ScheduleFit(DataGrid dataGrid)
    {
        dataGrid.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => FitHeaders(dataGrid));
    }

    public static void FitHeaders(DataGrid dataGrid)
    {
        if (!GetIsEnabled(dataGrid) || dataGrid.Columns.Count == 0)
            return;
        var typeface = new Typeface(
            dataGrid.FontFamily,
            dataGrid.FontStyle,
            dataGrid.FontWeight,
            dataGrid.FontStretch);
        var dpi = VisualTreeHelper.GetDpi(dataGrid).PixelsPerDip;
        var fontSize = dataGrid.FontSize;
        foreach (var column in dataGrid.Columns)
        {
            var headerText = column.Header?.ToString();
            if (string.IsNullOrWhiteSpace(headerText))
                continue;
            var formatted = new FormattedText(
                headerText,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                dpi);
            var needed = Math.Ceiling(
                formatted.WidthIncludingTrailingWhitespace + HeaderHorizontalPadding + SortGlyphExtra);
            if (column.MinWidth < needed)
                column.MinWidth = needed;
            if (column.Width.IsAbsolute && column.Width.Value < needed)
                column.Width = new DataGridLength(needed);
        }
    }
}
