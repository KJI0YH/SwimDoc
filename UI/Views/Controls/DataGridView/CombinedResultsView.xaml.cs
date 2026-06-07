using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using UI.Resources;
using UI.ViewModels.Pages;

namespace UI.Views.Controls.DataGridView;

public partial class CombinedResultsView : UserControl
{
    private static readonly CombinedResultsEventScoringConverter EventScoringConverter = new();
    private static readonly SolidColorBrush NonScoringCellBrush = new(Color.FromRgb(0xD3, 0xD3, 0xD3));
    private CombinedResultsViewModel? _subscribedViewModel;
    private DataGrid? _grid;
    public CombinedResultsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void CombinedResultsGrid_Loaded(object sender, RoutedEventArgs e)
    {
        _grid = (DataGrid)sender;
        UpdateColumns();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _subscribedViewModel = e.NewValue as CombinedResultsViewModel;
        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateColumns();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CombinedResultsViewModel.EventColumns) or nameof(CombinedResultsViewModel.Rows))
            UpdateColumns();
    }

    private void UpdateColumns()
    {
        if (_grid is null)
            return;
        _grid.Columns.Clear();
        if (DataContext is not CombinedResultsViewModel viewModel)
            return;
        _grid.Columns.Add(CreateTextColumn(Strings.Results_Col_Place, nameof(CombinedResultRow.PlaceDisplay), 80));
        _grid.Columns.Add(CreateTextColumn(Strings.Results_Col_Participant, nameof(CombinedResultRow.ParticipantName), 220));
        _grid.Columns.Add(CreateTextColumn(Strings.Fixation_Col_BirthYear, nameof(CombinedResultRow.YearOfBirth), 120));
        _grid.Columns.Add(CreateTextColumn(Strings.Results_Col_Team, nameof(CombinedResultRow.ClubName), 180));
        foreach (var column in viewModel.EventColumns)
        {
            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = column.Header,
                Width = 110,
                Binding = new Binding($"[{column.EventId}]")
                {
                    TargetNullValue = string.Empty
                },
                CellStyle = CreateNonScoringCellStyle(column.EventId)
            });
        }
        _grid.Columns.Add(CreateTextColumn(Strings.Results_Col_Total, nameof(CombinedResultRow.TotalPoints), 90));
    }

    private static Style CreateNonScoringCellStyle(int eventId)
    {
        var style = new Style(typeof(DataGridCell));
        var trigger = new DataTrigger
        {
            Binding = new Binding(".")
            {
                Converter = EventScoringConverter,
                ConverterParameter = eventId
            },
            Value = false
        };
        trigger.Setters.Add(new Setter(Control.BackgroundProperty, NonScoringCellBrush));
        style.Triggers.Add(trigger);
        return style;
    }

    private static DataGridTextColumn CreateTextColumn(string header, string bindingPath, double width) =>
        new()
        {
            Header = header,
            Width = width,
            Binding = new Binding(bindingPath)
        };
}
