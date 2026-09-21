using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using DataLayer.EfClasses;
using UI.Helpers.Controls;
using UI.Resources;
using UI.ViewModels.Pages;
using WpfUi = Wpf.Ui.Controls;

namespace UI.Views.Controls.RankTimesSettingsView;

public partial class RankTimesSettingsView : UserControl
{
    private const double DistanceColumnPadding = 24;

    public static readonly DependencyProperty ShowHeaderProperty =
        DependencyProperty.Register(
            nameof(ShowHeader),
            typeof(bool),
            typeof(RankTimesSettingsView),
            new PropertyMetadata(true));

    public bool ShowHeader
    {
        get => (bool)GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    public RankTimesSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ConfigureGrid(ScmMenGrid);
        ConfigureGrid(ScmWomenGrid);
        ConfigureGrid(LcmMenGrid);
        ConfigureGrid(LcmWomenGrid);
        ScheduleFitDistanceColumns();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        ScheduleFitDistanceColumns();

    private void LayoutRoot_OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        ScheduleFitDistanceColumns();

    private void RankTimesTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ScheduleFitDistanceColumns();

    private void ScheduleFitDistanceColumns()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, FitDistanceColumns);
    }

    private void FitDistanceColumns()
    {
        var grid = GetSelectedGrid();
        if (grid is null)
            return;
        RefreshHeaders(grid);
        FitDistanceColumn(grid);
    }

    private DataGrid? GetSelectedGrid() =>
        RankTimesTabs.SelectedIndex switch
        {
            0 => ScmMenGrid,
            1 => ScmWomenGrid,
            2 => LcmMenGrid,
            3 => LcmWomenGrid,
            _ => null
        };

    private static void RefreshHeaders(DataGrid grid)
    {
        if (grid.Columns.Count < 9)
            return;
        grid.Columns[0].Header = Strings.BaseTimes_Col_Stroke;
        grid.Columns[1].Header = Strings.GetEnumDisplay(Category.IMoS);
        grid.Columns[2].Header = Strings.GetEnumDisplay(Category.MoS);
        grid.Columns[3].Header = Strings.GetEnumDisplay(Category.CMoS);
        grid.Columns[4].Header = Strings.GetEnumDisplay(Category.FirstAdult);
        grid.Columns[5].Header = Strings.GetEnumDisplay(Category.SecondAdult);
        grid.Columns[6].Header = Strings.GetEnumDisplay(Category.ThirdAdult);
        grid.Columns[7].Header = Strings.GetEnumDisplay(Category.FirstJunior);
        grid.Columns[8].Header = Strings.GetEnumDisplay(Category.SecondJunior);
    }

    private static void ConfigureGrid(DataGrid grid)
    {
        if (grid.Columns.Count > 0)
            return;
        grid.Columns.Add(CreateDistanceColumn());
        grid.Columns.Add(CreateRankColumn(Strings.GetEnumDisplay(Category.IMoS), nameof(RankTimeTableRowViewModel.MsmkText)));
        grid.Columns.Add(CreateRankColumn(Strings.GetEnumDisplay(Category.MoS), nameof(RankTimeTableRowViewModel.MsText)));
        grid.Columns.Add(CreateRankColumn(Strings.GetEnumDisplay(Category.CMoS), nameof(RankTimeTableRowViewModel.KmsText)));
        grid.Columns.Add(CreateRankColumn(Strings.GetEnumDisplay(Category.FirstAdult), nameof(RankTimeTableRowViewModel.IText)));
        grid.Columns.Add(CreateRankColumn(Strings.GetEnumDisplay(Category.SecondAdult), nameof(RankTimeTableRowViewModel.IiText)));
        grid.Columns.Add(CreateRankColumn(Strings.GetEnumDisplay(Category.ThirdAdult), nameof(RankTimeTableRowViewModel.IiiText)));
        grid.Columns.Add(CreateRankColumn(Strings.GetEnumDisplay(Category.FirstJunior), nameof(RankTimeTableRowViewModel.IYunText)));
        grid.Columns.Add(CreateRankColumn(Strings.GetEnumDisplay(Category.SecondJunior), nameof(RankTimeTableRowViewModel.IiYunText)));

        DependencyPropertyDescriptor
            .FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid))
            ?.AddValueChanged(grid, (_, _) =>
            {
                if (Window.GetWindow(grid) is null)
                    return;
                grid.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => FitDistanceColumn(grid));
            });
    }

    private static DataGridTextColumn CreateDistanceColumn() =>
        new()
        {
            Header = Strings.BaseTimes_Col_Stroke,
            Binding = new Binding(nameof(RankTimeTableRowViewModel.Name)),
            IsReadOnly = true,
            Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
        };

    private static DataGridTemplateColumn CreateRankColumn(string header, string textProperty)
    {
        var textBoxFactory = new FrameworkElementFactory(typeof(WpfUi.TextBox));
        textBoxFactory.SetValue(FrameworkElement.StyleProperty,
            Application.Current.TryFindResource("SwimDocDataGridInlineTextBoxStyle"));
        textBoxFactory.SetValue(Control.PaddingProperty, new Thickness(2, 0, 2, 0));
        textBoxFactory.SetValue(FixationTimeInputNavigation.IsEnabledProperty, true);
        textBoxFactory.SetBinding(
            WpfUi.TextBox.TextProperty,
            new Binding(textProperty) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        return new DataGridTemplateColumn
        {
            Header = header,
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 0,
            CellTemplate = new DataTemplate { VisualTree = textBoxFactory }
        };
    }

    private static void FitDistanceColumn(DataGrid grid)
    {
        if (grid.Columns.Count == 0)
            return;
        var distanceColumn = grid.Columns[0];
        var typeface = new Typeface(
            grid.FontFamily,
            grid.FontStyle,
            grid.FontWeight,
            grid.FontStretch);
        var dpi = VisualTreeHelper.GetDpi(grid).PixelsPerDip;
        var fontSize = grid.FontSize;
        var maxWidth = MeasureText(distanceColumn.Header?.ToString() ?? string.Empty, typeface, fontSize, dpi);

        if (grid.ItemsSource is IEnumerable items)
        {
            foreach (var item in items)
            {
                if (item is not RankTimeTableRowViewModel row || string.IsNullOrEmpty(row.Name))
                    continue;
                maxWidth = Math.Max(maxWidth, MeasureText(row.Name, typeface, fontSize, dpi));
            }
        }

        var width = Math.Ceiling(maxWidth + DistanceColumnPadding);
        distanceColumn.MinWidth = width;
        distanceColumn.Width = new DataGridLength(width);
    }

    private static double MeasureText(string text, Typeface typeface, double fontSize, double dpi)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.Black,
            dpi);
        return formatted.WidthIncludingTrailingWhitespace;
    }
}
