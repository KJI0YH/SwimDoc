using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using UI.Models.CombinedResults;
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

    private void CombinedResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid || DataContext is not CombinedResultsViewModel viewModel)
            return;
        if (DataGridRowSelectionHelper.TryGetRowItem(dataGrid, e, out CombinedResultRow? row))
            viewModel.SelectedRow = row;
        if (!viewModel.OpenAthleteDetailsCommand.CanExecute(null))
            return;
        viewModel.OpenAthleteDetailsCommand.Execute(null);
        e.Handled = true;
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
        _grid.Columns.Add(CreateTextColumn(Strings.Athletes_Col_Category, nameof(CombinedResultRow.Category), 100));
        _grid.Columns.Add(CreateTextColumn(Strings.Results_Col_Team, nameof(CombinedResultRow.ClubName), 180));
        foreach (var column in viewModel.EventColumns)
        {
            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = column.Header,
                Width = 110,
                Binding = new Binding($"[{column.SwimStyleId}]")
                {
                    TargetNullValue = string.Empty
                },
                CellStyle = CreateNonScoringCellStyle(column.SwimStyleId)
            });
        }
        _grid.Columns.Add(CreateTextColumn(Strings.Results_Col_Total, nameof(CombinedResultRow.TotalPoints), 90));
    }

    private static Style CreateNonScoringCellStyle(int eventId)
    {
        var style = new Style(typeof(DataGridCell), (Style)Application.Current.FindResource(typeof(DataGridCell)));
        var nonScoringTrigger = new MultiDataTrigger();
        nonScoringTrigger.Conditions.Add(new Condition
        {
            Binding = new Binding(".")
            {
                Converter = EventScoringConverter,
                ConverterParameter = eventId
            },
            Value = false
        });
        nonScoringTrigger.Conditions.Add(new Condition
        {
            Binding = new Binding("IsSelected")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1)
            },
            Value = false
        });
        nonScoringTrigger.Setters.Add(new Setter(Control.BackgroundProperty, NonScoringCellBrush));
        style.Triggers.Add(nonScoringTrigger);
        return style;
    }

    private static DataGridTextColumn CreateTextColumn(string header, string bindingPath, double width) =>
        new()
        {
            Header = header,
            Width = width,
            IsReadOnly = true,
            Binding = new Binding(bindingPath)
        };
}
