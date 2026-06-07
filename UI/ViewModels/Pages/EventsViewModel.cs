using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.SwimStyleService;
using ServiceLayer.ReportGeneratorService;
using UI.Resources;
using UI.ViewModels.Pages.Data;
using UI.Models.Rows;
using UI.Models.Rows.Projections;
using UI.ViewModels.Dialogs.HeatAllocationParameters;
using UI.ViewModels.Dialogs.ReportGeneration;
using UI.ViewModels.Dialogs.StartTimeCalculation;
using UI.Views.Dialogs.Markers.AddEdit;
using HeatAllocationParametersWindow = UI.Views.Dialogs.Markers.HeatAllocationParameters.HeatAllocationParametersWindow;
using ReportGenerationWindow = UI.Views.Dialogs.Markers.ReportGeneration.ReportGenerationWindow;
using StartTimeCalculationWindow = UI.Views.Dialogs.Markers.StartTimeCalculation.StartTimeCalculationWindow;

namespace UI.ViewModels.Pages;

public partial class EventsViewModel : DataViewModel<SwimEvent, SwimEventRowView, int?>
{
    protected override PagingPage PagingSettingsPage => PagingPage.Events;
    private IEventService EventService =>
        App.Current.Services.GetRequiredService<IEventService>();
    private readonly IAddEditWindowFactory _windowFactory;
    [ObservableProperty] private ObservableCollection<EventFilterOption<int>> _distanceFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<Stroke>> _strokeFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<Gender>> _genderFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<SwimEventStatus>> _statusFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<EventRound>> _roundFilterOptions = new();
    [ObservableProperty] private bool _isFiltersPanelVisible;
    public string DistanceFilterText => GetFilterText(DistanceFilterOptions, Strings.Filters_Distance);
    public string StrokeFilterText => GetFilterText(StrokeFilterOptions, Strings.Filters_Stroke);
    public string GenderFilterText => GetFilterText(GenderFilterOptions, Strings.Filters_Gender);
    public string StatusFilterText => GetFilterText(StatusFilterOptions, Strings.Filters_Status);
    public string RoundFilterText => GetFilterText(RoundFilterOptions, Strings.Filters_Round);
    private bool _filterOptionsInitialized;

    public EventsViewModel(IEventService eventService) : base(eventService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        PropertyChanged += OnViewModelPropertyChanged;
        App.Current.Services.GetRequiredService<ILocalizationService>().CultureChanged += OnCultureChanged;
    }

    protected override async Task PrepareBeforeLoadAsync()
    {
        await EnsureFilterOptionsInitializedAsync();
    }

    protected override void ResetForNewCompetition()
    {
        base.ResetForNewCompetition();
        _filterOptionsInitialized = false;
    }

    private void OnCultureChanged(CultureInfo culture)
    {
        RefreshLocalizedEnumFilterOptions();
        ReloadFromFirstPage();
    }

    private void RefreshLocalizedEnumFilterOptions()
    {
        foreach (var option in StrokeFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);
        foreach (var option in GenderFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);
        foreach (var option in StatusFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);
        foreach (var option in RoundFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);
        OnPropertyChanged(nameof(StrokeFilterText));
        OnPropertyChanged(nameof(GenderFilterText));
        OnPropertyChanged(nameof(StatusFilterText));
        OnPropertyChanged(nameof(RoundFilterText));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedItems))
        {
            HeatAllocationCommand.NotifyCanExecuteChanged();
            GenerateReportsCommand.NotifyCanExecuteChanged();
            CalculateStartTimesCommand.NotifyCanExecuteChanged();
        }
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Order", Strings.Events_Col_Order, 80,
            ColumnConfiguration<SwimEvent>.SortBy(e => e.Order)));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Date", Strings.Events_Col_Date, 85,
            ColumnConfiguration<SwimEvent>.SortBy(e => e.Date)));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Time", Strings.Events_Col_Time, 57,
            ColumnConfiguration<SwimEvent>.SortBy(e => e.Time)));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Round", Strings.Events_Col_Round, 150,
            ColumnConfiguration<SwimEvent>.SortBy(e => e.Round)));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("SwimStyle", Strings.Events_Col_Distance, 300,
            ColumnConfiguration<SwimEvent>.SortBy(
                e => e.SwimStyle.Distance,
                e => e.SwimStyle.Stroke,
                e => e.SwimStyle.RelayCount))
        {
            Converter = EntityDisplayConverter.Instance,
            ConverterParameter = EntityDisplayConverter.SwimStyleKind
        });
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("AgeGroup", Strings.Events_Col_AgeGroup, 300,
            ColumnConfiguration<SwimEvent>.SortBy(e => e.AgeGroup!.Name))
        {
            Converter = EntityDisplayConverter.Instance,
            ConverterParameter = EntityDisplayConverter.AgeGroupKind
        });
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Lanes", Strings.Events_Col_Lanes, 80,
            ColumnConfiguration<SwimEvent>.SortBy(e => e.LaneMin, e => e.LaneMax)));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Status", Strings.Events_Col_Status, 210,
            ColumnConfiguration<SwimEvent>.SortBy(e => e.Status)));
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query) =>
        query.OrderBy(se => se.Order);

    protected override async Task<List<SwimEventRowView>> LoadPageRowsAsync(IQueryable<SwimEvent> query)
    {
        var projections = await RowProjectionQueries.SelectSwimEvent(query).ToListAsync();
        return projections.Select(SwimEventRowView.FromProjection).ToList();
    }

    protected override IQueryable<SwimEvent> ApplySearch(IQueryable<SwimEvent> query) =>
        ApplySelectedFilters(query);

    private IQueryable<SwimEvent> ApplySelectedFilters(IQueryable<SwimEvent> query)
    {
        var distances = DistanceFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (distances.Length > 0)
            query = query.Where(e => distances.Contains(e.SwimStyle.Distance));
        var strokes = StrokeFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (strokes.Length > 0)
            query = query.Where(e => strokes.Contains(e.SwimStyle.Stroke));
        var genders = GenderFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (genders.Length > 0)
            query = query.Where(e => genders.Contains(e.AgeGroup.Gender));
        var statuses = StatusFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (statuses.Length > 0)
            query = query.Where(e => statuses.Contains(e.Status));
        var rounds = RoundFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (rounds.Length > 0)
            query = query.Where(e => rounds.Contains(e.Round));
        return query;
    }

    private async Task EnsureFilterOptionsInitializedAsync()
    {
        if (_filterOptionsInitialized)
            return;
        _filterOptionsInitialized = true;
        await InitializeFilterOptionsAsync();
        SubscribeFilterOptions();
    }

    private async Task InitializeFilterOptionsAsync()
    {
        var swimStyleService = App.Current.Services.GetRequiredService<ISwimStyleService>();
        var distances = await swimStyleService.Query()
            .Select(swimStyle => swimStyle.Distance)
            .Distinct()
            .OrderBy(distance => distance)
            .ToListAsync();
        DistanceFilterOptions = new ObservableCollection<EventFilterOption<int>>(
            distances.Select(distance => new EventFilterOption<int>(
                distance,
                string.Format(Strings.Distance_MetersFormat, distance))));
        StrokeFilterOptions = new ObservableCollection<EventFilterOption<Stroke>>(
            Enum.GetValues<Stroke>().Select(stroke =>
                new EventFilterOption<Stroke>(stroke, Strings.GetEnumDisplay(stroke))));
        GenderFilterOptions = new ObservableCollection<EventFilterOption<Gender>>(
            Enum.GetValues<Gender>().Select(gender =>
                new EventFilterOption<Gender>(gender, Strings.GetEnumDisplay(gender))));
        StatusFilterOptions = new ObservableCollection<EventFilterOption<SwimEventStatus>>(
            Enum.GetValues<SwimEventStatus>().Select(status =>
                new EventFilterOption<SwimEventStatus>(status, Strings.GetEnumDisplay(status))));
        RoundFilterOptions = new ObservableCollection<EventFilterOption<EventRound>>(
            Enum.GetValues<EventRound>().Select(round =>
                new EventFilterOption<EventRound>(round, Strings.GetEnumDisplay(round))));
    }

    private void SubscribeFilterOptions()
    {
        SubscribeFilterOptions(DistanceFilterOptions);
        SubscribeFilterOptions(StrokeFilterOptions);
        SubscribeFilterOptions(GenderFilterOptions);
        SubscribeFilterOptions(StatusFilterOptions);
        SubscribeFilterOptions(RoundFilterOptions);
    }

    private void SubscribeFilterOptions<T>(IEnumerable<EventFilterOption<T>> options)
    {
        foreach (var option in options)
            option.PropertyChanged += OnFilterOptionPropertyChanged;
    }

    private void OnFilterOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IEventFilterOption.IsSelected))
            return;
        OnPropertyChanged(nameof(DistanceFilterText));
        OnPropertyChanged(nameof(StrokeFilterText));
        OnPropertyChanged(nameof(GenderFilterText));
        OnPropertyChanged(nameof(StatusFilterText));
        OnPropertyChanged(nameof(RoundFilterText));
        ClearFiltersCommand.NotifyCanExecuteChanged();
        ReloadFromFirstPage();
    }

    [RelayCommand]
    private void ToggleFiltersPanel() => IsFiltersPanelVisible = !IsFiltersPanelVisible;

    [RelayCommand(CanExecute = nameof(HasActiveFilters))]
    private void ClearFilters()
    {
        ClearFilterOptions(DistanceFilterOptions);
        ClearFilterOptions(StrokeFilterOptions);
        ClearFilterOptions(GenderFilterOptions);
        ClearFilterOptions(StatusFilterOptions);
        ClearFilterOptions(RoundFilterOptions);
    }

    private bool HasActiveFilters() =>
        DistanceFilterOptions.Any(option => option.IsSelected) ||
        StrokeFilterOptions.Any(option => option.IsSelected) ||
        GenderFilterOptions.Any(option => option.IsSelected) ||
        StatusFilterOptions.Any(option => option.IsSelected) ||
        RoundFilterOptions.Any(option => option.IsSelected);

    private static void ClearFilterOptions<T>(IEnumerable<EventFilterOption<T>> options)
    {
        foreach (var option in options)
            option.IsSelected = false;
    }

    private void ReloadFromFirstPage()
    {
        if (CurrentPage == 0)
            LoadDataCommand.Execute(null);
        else
            CurrentPage = 0;
    }

    private static string GetFilterText<T>(IEnumerable<EventFilterOption<T>> options, string placeholder)
    {
        var selected = options.Where(option => option.IsSelected).Select(option => option.DisplayText).ToArray();
        return selected.Length == 0 ? placeholder : string.Join(", ", selected);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<EventAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }

    [RelayCommand(CanExecute = nameof(CanAllocateHeats))]
    private async Task HeatAllocation()
    {
        if (SelectedItems.Count == 0)
            return;
        var dialog = _windowFactory.CreateAndShowAndReturn<HeatAllocationParametersWindow>();
        if (dialog.DataContext is not IWindowResult { Result: HeatAllocationParametersResult result })
            return;
        var deleteConfirmation = App.Current.Services.GetRequiredService<IConfirmDialogService>();
        var events = SelectedItems.Select(row => row.Entity).OrderBy(swimEvent => swimEvent.Order).ToList();
        await using var batch = new HeatAllocationBatchSession(result.HeatOrder, result.MinHeatSize);
        var runResult = await RunMultiItemOperationAsync(
            Strings.Operation_HeatAllocation_Header,
            Strings.Operation_HeatAllocation_Preparing_MessageFormat,
            Strings.Operation_HeatAllocation_Processing_MessageFormat,
            Strings.Operation_HeatAllocation_Finished_MessageFormat,
            Strings.Operation_HeatAllocation_Canceled_Header,
            events,
            async (swimEvent, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                var confirmed = await Application.Current.Dispatcher.InvokeAsync(
                    () => deleteConfirmation.ConfirmHeatReformIfOfficialResultsExistAsync(
                        swimEvent.Id,
                        EntityDisplayFormatter.FormatSwimEvent(swimEvent)));
                if (!await confirmed)
                    return OperationItemOutcome.Skipped();
                ct.ThrowIfCancellationRequested();
                return batch.AllocateEvent(swimEvent.Id);
            });
        await LoadDataAsync();
    }

    private bool CanAllocateHeats() => SelectedItems.Count > 0 && !IsOperationRunning;

    [RelayCommand(CanExecute = nameof(CanGenerateReports))]
    private async Task GenerateReports()
    {
        if (SelectedItems.Count == 0)
            return;
        var dialog = _windowFactory.CreateAndShowAndReturn<ReportGenerationWindow>();
        if (dialog.DataContext is not IWindowResult { Result: ReportGenerationResult result })
            return;
        var options = new ReportExportOptions
        {
            SwimEventIds = SelectedItems.Select(se => se.Id).ToList(),
            OutputFilePath = result.OutputFilePath,
            IncludeEntryList = result.IncludeEntryList,
            IncludeStartList = result.IncludeStartList,
            IncludeFinishList = result.IncludeFinishList
        };
        await RunSingleOperationAsync(
            Strings.Operation_Reports_Header,
            Strings.Operation_Reports_Running_Message,
            string.Format(Strings.Operation_Reports_Finished_MessageFormat, Path.GetFileName(result.OutputFilePath)),
            Strings.Operation_Reports_Canceled_Header,
            async ct =>
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
                try
                {
                    var tempOptions = new ReportExportOptions
                    {
                        SwimEventIds = options.SwimEventIds,
                        OutputFilePath = tempPath,
                        IncludeEntryList = options.IncludeEntryList,
                        IncludeStartList = options.IncludeStartList,
                        IncludeFinishList = options.IncludeFinishList
                    };
                    using var scope = App.Current.Services.CreateScope();
                    scope.ServiceProvider.GetRequiredService<IReportExportService>()
                        .ExportToExcel(tempOptions);
                    ct.ThrowIfCancellationRequested();
                    if (File.Exists(options.OutputFilePath))
                        File.Delete(options.OutputFilePath);
                    File.Move(tempPath, options.OutputFilePath);
                    return OperationItemOutcome.Success();
                }
                catch (OperationCanceledException)
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                    throw;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                    return OperationItemOutcome.Failed([Strings.Dialog_Error_FileBusyOrUnavailable]);
                }
            });
    }

    private bool CanGenerateReports() => SelectedItems.Count > 0 && !IsOperationRunning;

    [RelayCommand(CanExecute = nameof(CanCalculateStartTimes))]
    private async Task CalculateStartTimesAsync()
    {
        if (SelectedItems.Count == 0)
            return;
        var dialog = _windowFactory.CreateAndShowAndReturn<StartTimeCalculationWindow>();
        if (dialog.DataContext is not IWindowResult { Result: StartTimeCalculationResult result })
            return;
        var swimEventIds = SelectedItems
            .OrderBy(swimEvent => swimEvent.Order)
            .Select(swimEvent => swimEvent.Id)
            .ToList();
        await RunSingleOperationAsync(
            Strings.Operation_StartTimes_Header,
            Strings.Operation_StartTimes_Running_Message,
            string.Format(Strings.Operation_StartTimes_Finished_MessageFormat, swimEventIds.Count),
            Strings.Operation_StartTimes_Canceled_Header,
            async ct =>
            {
                using var scope = App.Current.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IEventService>()
                    .CalculateStartTimesAsync(swimEventIds, result.ToParameters(), ct);
                return OperationItemOutcome.Success();
            });
        await LoadDataAsync();
    }

    private bool CanCalculateStartTimes() => SelectedItems.Count > 0 && !IsOperationRunning;
}
