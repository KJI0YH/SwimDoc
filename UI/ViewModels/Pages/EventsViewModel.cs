using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using BizLogic.HeatLogic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.ReportGeneratorService;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.ViewModels.Windows.HeatAllocationParameters;
using UI.ViewModels.Windows.ReportGeneration;
using UI.ViewModels.Windows.StartTimeCalculation;
using UI.Views.Windows.AddEdit;
using HeatAllocationParametersWindow = UI.Views.Windows.HeatAllocationParameters.HeatAllocationParametersWindow;
using ReportGenerationWindow = UI.Views.Windows.ReportGeneration.ReportGenerationWindow;
using StartTimeCalculationWindow = UI.Views.Windows.StartTimeCalculation.StartTimeCalculationWindow;

namespace UI.ViewModels.Pages;

public partial class EventsViewModel : DataViewModel<SwimEvent, int?>
{
    private readonly IEventService _eventService;
    private readonly IHeatService _heatService;
    private readonly IAddEditWindowFactory _windowFactory;
    private readonly IReportExportService _reportExportService;

    [ObservableProperty] private ObservableCollection<EventFilterOption<int>> _distanceFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<Stroke>> _strokeFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<Gender>> _genderFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<SwimEventStatus>> _statusFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<EventRound>> _roundFilterOptions = new();
    [ObservableProperty] private bool _isFiltersPanelVisible;

    public string DistanceFilterText => GetFilterText(DistanceFilterOptions, "Дистанция");
    public string StrokeFilterText => GetFilterText(StrokeFilterOptions, "Стиль");
    public string GenderFilterText => GetFilterText(GenderFilterOptions, "Пол");
    public string StatusFilterText => GetFilterText(StatusFilterOptions, "Статус");
    public string RoundFilterText => GetFilterText(RoundFilterOptions, "Этап");

    public EventsViewModel(IEventService eventService)
        : this(eventService, App.Current.Services.GetRequiredService<IHeatService>())
    {
    }

    public EventsViewModel(IEventService eventService, IHeatService heatService) : base(eventService)
    {
        _eventService = eventService;
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        _heatService = heatService;
        _reportExportService = App.Current.Services.GetRequiredService<IReportExportService>();
        PropertyChanged += OnViewModelPropertyChanged;
        InitializeFilterOptions();
        SubscribeFilterOptions();
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

        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Order", "Порядок", 80));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("DisplayDate", "Дата", 100,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.Date)
                    : query.OrderByDescending(e => e.Date);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("DisplayTime", "Время", 120));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Round", "Этап", 150));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("SwimStyle.DisplayName", "Дистанция", 300,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.SwimStyle.Distance).ThenBy(e => e.SwimStyle.Stroke)
                        .ThenBy(e => e.SwimStyle.RelayCount)
                    : query.OrderByDescending(e => e.SwimStyle.Distance).ThenByDescending(e => e.SwimStyle.Stroke)
                        .ThenBy(e => e.SwimStyle.RelayCount);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("AgeGroup.DisplayName", "Возрастная группа", 300,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.AgeGroup.Name)
                    : query.OrderByDescending(e => e.AgeGroup.Name);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("DisplayLanes", "Дорожки", 100,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.LaneMin).ThenBy(e => e.LaneMax)
                    : query.OrderByDescending(e => e.LaneMin).ThenByDescending(e => e.LaneMax);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("DisplayStatus", "Статус", 120));
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query
            .OrderBy(se => se.Order)
            .Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle)
            .Include(swimEvent => swimEvent.Heats);
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

    private void InitializeFilterOptions()
    {
        var distances = _eventService.Query()
            .Select(e => e.SwimStyle.Distance)
            .Distinct()
            .OrderBy(distance => distance)
            .ToList();

        DistanceFilterOptions = new ObservableCollection<EventFilterOption<int>>(
            distances.Select(distance => new EventFilterOption<int>(distance, $"{distance}м")));

        StrokeFilterOptions = new ObservableCollection<EventFilterOption<Stroke>>(
            Enum.GetValues<Stroke>().Select(stroke =>
                new EventFilterOption<Stroke>(stroke, EnumDisplay.GetDescription(stroke))));

        GenderFilterOptions = new ObservableCollection<EventFilterOption<Gender>>(
            Enum.GetValues<Gender>().Select(gender =>
                new EventFilterOption<Gender>(gender, EnumDisplay.GetDescription(gender))));

        StatusFilterOptions = new ObservableCollection<EventFilterOption<SwimEventStatus>>(
            Enum.GetValues<SwimEventStatus>().Select(status =>
                new EventFilterOption<SwimEventStatus>(status, status.ToString())));

        RoundFilterOptions = new ObservableCollection<EventFilterOption<EventRound>>(
            Enum.GetValues<EventRound>().Select(round =>
                new EventFilterOption<EventRound>(round, EnumDisplay.GetDescription(round))));
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

        var window = _windowFactory.CreateAndShowAndReturn<HeatAllocationParametersWindow>();
        if (window.DataContext is not IWindowResult { Result: HeatAllocationParametersResult result })
            return;

        var deleteConfirmation = App.Current.Services.GetRequiredService<IConfirmDialogService>();

        foreach (var swimEvent in SelectedItems)
        {
            if (!await deleteConfirmation.ConfirmHeatReformIfOfficialResultsExistAsync(swimEvent.Id))
                continue;

            var parameters = new HeatAllocationParameters(swimEvent.Id, result.HeatOrder, result.MinHeatSize);
            _heatService.AllocateEntriesToHeats(parameters);
        }

        _ = LoadDataAsync();
    }

    private bool CanAllocateHeats()
    {
        return SelectedItems.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanGenerateReports))]
    private async Task GenerateReports()
    {
        if (SelectedItems.Count == 0)
            return;

        var window = _windowFactory.CreateAndShowAndReturn<ReportGenerationWindow>();
        if (window.DataContext is not IWindowResult { Result: ReportGenerationResult result })
            return;

        var options = new ReportExportOptions
        {
            SwimEventIds = SelectedItems.Select(se => se.Id).ToList(),
            OutputFilePath = result.OutputFilePath,
            IncludeEntryList = result.IncludeEntryList,
            IncludeStartList = result.IncludeStartList,
            IncludeFinishList = result.IncludeFinishList
        };

        try
        {
            _reportExportService.ExportToExcel(options);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var dialogs = App.Current.Services.GetRequiredService<IErrorDialogService>();
            await dialogs.ShowErrorAsync(
                title: "Не удалось сохранить отчёт",
                message: $"Файл занят другим процессом или недоступен");
        }
    }

    private bool CanGenerateReports()
    {
        return SelectedItems.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanCalculateStartTimes))]
    private async Task CalculateStartTimesAsync()
    {
        if (SelectedItems.Count == 0)
            return;

        var window = _windowFactory.CreateAndShowAndReturn<StartTimeCalculationWindow>();
        if (window.DataContext is not IWindowResult { Result: StartTimeCalculationResult result })
            return;

        var swimEventIds = SelectedItems
            .OrderBy(swimEvent => swimEvent.Order)
            .Select(swimEvent => swimEvent.Id)
            .ToList();

        await _eventService.CalculateStartTimesAsync(swimEventIds, result.ToParameters());
        await LoadDataAsync();
    }

    private bool CanCalculateStartTimes() => SelectedItems.Count > 0;
}