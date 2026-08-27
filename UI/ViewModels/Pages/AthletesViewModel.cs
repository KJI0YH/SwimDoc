using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryService;
using UI.Helpers.Threading;
using UI.Resources;
using UI.ViewModels.Pages.Data;
using UI.Models.Rows;
using UI.Models.Rows.Projections;
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Pages;

public partial class AthletesViewModel : DataViewModel<Athlete, AthleteRowView, int?>
{
    public readonly record struct AgeGroupFilterValue(int Id, Gender Gender, int BirthYearMin, int BirthYearMax);

    protected override PagingPage PagingSettingsPage => PagingPage.Athletes;
    private readonly IAddEditWindowFactory _windowFactory;
    private bool _filterOptionsInitialized;
    private bool _cultureSubscribed;

    [ObservableProperty] private ObservableCollection<EventFilterOption<AgeGroupFilterValue>> _ageGroupFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<Gender>> _genderFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<Category>> _categoryFilterOptions = new();
    [ObservableProperty] private ObservableCollection<EventFilterOption<int?>> _clubFilterOptions = new();
    [ObservableProperty] private int? _birthYearFrom;
    [ObservableProperty] private int? _birthYearTo;
    [ObservableProperty] private bool _isFiltersPanelVisible;

    public string AgeGroupFilterText => GetFilterText(AgeGroupFilterOptions, Strings.Filters_AgeGroup);
    public string GenderFilterText => GetFilterText(GenderFilterOptions, Strings.Filters_Gender);
    public string CategoryFilterText => GetFilterText(CategoryFilterOptions, Strings.Filters_Category);
    public string ClubFilterText => GetFilterText(ClubFilterOptions, Strings.Filters_Club);

    public AthletesViewModel(IAthleteService athleteService) : base(athleteService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    protected override async Task PrepareBeforeLoadAsync()
    {
        await EnsureFilterOptionsInitializedAsync();
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("LastName", Strings.Athletes_Col_LastName, 200,
            ColumnConfiguration<Athlete>.SortBy(e => e.LastName)));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("FirstName", Strings.Athletes_Col_FirstName, 200,
            ColumnConfiguration<Athlete>.SortBy(e => e.FirstName)));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("Gender", Strings.Athletes_Col_Gender, 90,
            ColumnConfiguration<Athlete>.SortBy(e => e.Gender)));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("YearOfBirth", Strings.Athletes_Col_BirthYear, 120,
            ColumnConfiguration<Athlete>.SortBy(e => e.YearOfBirth)));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("Category", Strings.Athletes_Col_Category, 100,
            ColumnConfiguration<Athlete>.SortBy(e => e.Category)));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("ClubName", Strings.Athletes_Col_Team, 300,
            ColumnConfiguration<Athlete>.SortBy(e => e.Club!.Name)));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("PointCount", Strings.Athletes_Col_Points, 80,
            ColumnConfiguration<Athlete>.SortBy(e =>
                e.Entries.Where(entry => entry.Scoring).Sum(entry => entry.Points ?? 0))));
    }

    protected override async Task<List<AthleteRowView>> LoadPageRowsAsync(
        IQueryable<Athlete> query,
        IServiceProvider serviceProvider)
    {
        var projections = await RowProjectionQueries.SelectAthlete(query).ToListAsync().ConfigureAwait(false);
        var athleteIds = projections.Select(projection => projection.Id).ToList();
        var dbContext = serviceProvider.GetRequiredService<EfCoreContext>();
        var pointCounts = await ScoringPointCountQueries
            .GetAthletePointCountsAsync(dbContext, athleteIds)
            .ConfigureAwait(false);
        return projections
            .Select(projection => AthleteRowView.FromProjection(
                projection,
                pointCounts.GetValueOrDefault(projection.Id)))
            .ToList();
    }

    protected override IQueryable<Athlete> ApplySearch(IQueryable<Athlete> query)
    {
        query = ApplySelectedFilters(query);
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;
        var trimmed = SearchText.Trim();
        if (int.TryParse(trimmed, out _) && !trimmed.Contains(' '))
            return query.Where(a => EF.Functions.Like(a.YearOfBirth.ToString(), $"%{trimmed}%"));
        var terms = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0)
            return query;
        foreach (var term in terms)
        {
            if (Strings.TryFindEnumByDisplayContains(term, out Gender gender))
            {
                query = query.Where(a => a.Gender == gender);
                continue;
            }
            query = query.Where(a =>
                SwimDocDbFunctions.ContainsIgnoreCase(a.FirstName, term) ||
                SwimDocDbFunctions.ContainsIgnoreCase(a.LastName, term) ||
                SwimDocDbFunctions.ContainsIgnoreCase(a.FirstName + " " + a.LastName, term) ||
                SwimDocDbFunctions.ContainsIgnoreCase(a.LastName + " " + a.FirstName, term) ||
                (a.Club != null && SwimDocDbFunctions.ContainsIgnoreCase(a.Club.Name, term)));
        }
        return query;
    }

    private IQueryable<Athlete> ApplySelectedFilters(IQueryable<Athlete> query)
    {
        var genders = GenderFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (genders.Length > 0)
            query = query.Where(a => genders.Contains(a.Gender));

        var categories = CategoryFilterOptions.Where(option => option.IsSelected).Select(option => option.Value)
            .ToArray();
        if (categories.Length > 0)
            query = query.Where(a => categories.Contains(a.Category));

        var clubIds = ClubFilterOptions.Where(option => option.IsSelected).Select(option => option.Value).ToArray();
        if (clubIds.Length > 0)
        {
            var includeNoClub = clubIds.Contains(null);
            var selectedClubIds = clubIds.Where(id => id.HasValue).Select(id => id!.Value).ToArray();
            query = query.Where(a =>
                (includeNoClub && a.ClubId == null) ||
                (a.ClubId != null && selectedClubIds.Contains(a.ClubId.Value)));
        }

        if (BirthYearFrom is int fromYear)
            query = query.Where(a => a.YearOfBirth >= fromYear);
        if (BirthYearTo is int toYear)
            query = query.Where(a => a.YearOfBirth <= toYear);

        var ageGroups = AgeGroupFilterOptions.Where(option => option.IsSelected).Select(option => option.Value)
            .ToArray();
        if (ageGroups.Length > 0)
            query = WhereMatchesAnyAgeGroup(query, ageGroups);

        return query;
    }

    private static IQueryable<Athlete> WhereMatchesAnyAgeGroup(
        IQueryable<Athlete> query,
        IReadOnlyList<AgeGroupFilterValue> ageGroups)
    {
        Expression? body = null;
        var parameter = Expression.Parameter(typeof(Athlete), "a");
        var yearOfBirth = Expression.Property(parameter, nameof(Athlete.YearOfBirth));
        var gender = Expression.Property(parameter, nameof(Athlete.Gender));

        foreach (var ageGroup in ageGroups)
        {
            var yearInRange = Expression.AndAlso(
                Expression.GreaterThanOrEqual(yearOfBirth, Expression.Constant(ageGroup.BirthYearMin)),
                Expression.LessThanOrEqual(yearOfBirth, Expression.Constant(ageGroup.BirthYearMax)));
            Expression genderMatches = ageGroup.Gender == Gender.Mixed
                ? Expression.Constant(true)
                : Expression.Equal(gender, Expression.Constant(ageGroup.Gender));
            var matches = Expression.AndAlso(yearInRange, genderMatches);
            body = body is null ? matches : Expression.OrElse(body, matches);
        }

        var predicate = Expression.Lambda<Func<Athlete, bool>>(body!, parameter);
        return query.Where(predicate);
    }

    private async Task EnsureFilterOptionsInitializedAsync()
    {
        if (_filterOptionsInitialized)
            return;
        _filterOptionsInitialized = true;
        await InitializeFilterOptionsAsync().ConfigureAwait(false);
        EnsureCultureSubscription();
    }

    private void EnsureCultureSubscription()
    {
        if (_cultureSubscribed)
            return;
        _cultureSubscribed = true;
        App.Current.Services.GetRequiredService<ILocalizationService>().CultureChanged += OnCultureChanged;
    }

    private void OnCultureChanged(CultureInfo culture)
    {
        if (!_filterOptionsInitialized)
            return;
        RefreshLocalizedFilterOptions();
        ReloadFromFirstPage();
    }

    private void RefreshLocalizedFilterOptions()
    {
        foreach (var option in GenderFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);
        foreach (var option in CategoryFilterOptions)
            option.DisplayText = Strings.GetEnumDisplay(option.Value);
        foreach (var option in ClubFilterOptions.Where(option => option.Value is null))
            option.DisplayText = Strings.Filters_Personal;
        OnPropertyChanged(nameof(GenderFilterText));
        OnPropertyChanged(nameof(CategoryFilterText));
        OnPropertyChanged(nameof(ClubFilterText));
        OnPropertyChanged(nameof(AgeGroupFilterText));
        _ = RefreshAgeGroupFilterDisplayTextsAsync();
    }

    private async Task RefreshAgeGroupFilterDisplayTextsAsync()
    {
        if (AgeGroupFilterOptions.Count == 0)
            return;
        var ageGroupService = App.Current.Services.GetRequiredService<IAgeGroupService>();
        var ageGroups = await ageGroupService.Query()
            .ToListAsync()
            .ConfigureAwait(false);
        var displayById = ageGroups.ToDictionary(
            ageGroup => ageGroup.Id,
            EntityDisplayFormatter.FormatAgeGroup);
        await DispatcherUiHelper.InvokeOnUiAsync(() =>
        {
            foreach (var option in AgeGroupFilterOptions)
            {
                if (displayById.TryGetValue(option.Value.Id, out var displayText))
                    option.DisplayText = displayText;
            }
            OnPropertyChanged(nameof(AgeGroupFilterText));
        });
    }

    protected override void ResetForNewCompetition()
    {
        base.ResetForNewCompetition();
        ResetFilterOptions();
    }

    protected void ResetFilterOptions()
    {
        _filterOptionsInitialized = false;
        UnsubscribeFilterOptions(AgeGroupFilterOptions);
        UnsubscribeFilterOptions(GenderFilterOptions);
        UnsubscribeFilterOptions(CategoryFilterOptions);
        UnsubscribeFilterOptions(ClubFilterOptions);
        BirthYearFrom = null;
        BirthYearTo = null;
    }

    private async Task InitializeFilterOptionsAsync()
    {
        var ageGroupService = App.Current.Services.GetRequiredService<IAgeGroupService>();
        var ageGroups = await ageGroupService.Query()
            .OrderBy(ageGroup => ageGroup.Name)
            .ThenBy(ageGroup => ageGroup.Gender)
            .ThenBy(ageGroup => ageGroup.BirthYearMin)
            .ToListAsync()
            .ConfigureAwait(false);
        var clubService = App.Current.Services.GetRequiredService<IClubService>();
        var clubs = await clubService.Query()
            .OrderBy(club => club.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        await DispatcherUiHelper.InvokeOnUiAsync(() =>
        {
            AgeGroupFilterOptions = new ObservableCollection<EventFilterOption<AgeGroupFilterValue>>(
                ageGroups.Select(ageGroup => new EventFilterOption<AgeGroupFilterValue>(
                    new AgeGroupFilterValue(
                        ageGroup.Id,
                        ageGroup.Gender,
                        ageGroup.BirthYearMin ?? 0,
                        ageGroup.BirthYearMax ?? int.MaxValue),
                    EntityDisplayFormatter.FormatAgeGroup(ageGroup))));
            GenderFilterOptions = new ObservableCollection<EventFilterOption<Gender>>(
                Enum.GetValues<Gender>()
                    .Where(gender => gender != Gender.Mixed)
                    .Select(gender => new EventFilterOption<Gender>(gender, Strings.GetEnumDisplay(gender))));
            CategoryFilterOptions = new ObservableCollection<EventFilterOption<Category>>(
                Enum.GetValues<Category>().Select(category =>
                    new EventFilterOption<Category>(category, Strings.GetEnumDisplay(category))));
            ClubFilterOptions = new ObservableCollection<EventFilterOption<int?>>(
                new[] { new EventFilterOption<int?>(null, Strings.Filters_Personal) }
                    .Concat(clubs.Select(club => new EventFilterOption<int?>(club.Id, club.Name))));
            SubscribeFilterOptions();
        });
    }

    private void SubscribeFilterOptions()
    {
        SubscribeFilterOptions(AgeGroupFilterOptions);
        SubscribeFilterOptions(GenderFilterOptions);
        SubscribeFilterOptions(CategoryFilterOptions);
        SubscribeFilterOptions(ClubFilterOptions);
    }

    private void SubscribeFilterOptions<T>(IEnumerable<EventFilterOption<T>> options)
    {
        foreach (var option in options)
            option.PropertyChanged += OnFilterOptionPropertyChanged;
    }

    private void UnsubscribeFilterOptions<T>(IEnumerable<EventFilterOption<T>> options)
    {
        foreach (var option in options)
            option.PropertyChanged -= OnFilterOptionPropertyChanged;
    }

    private void OnFilterOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IEventFilterOption.IsSelected))
            return;
        OnPropertyChanged(nameof(AgeGroupFilterText));
        OnPropertyChanged(nameof(GenderFilterText));
        OnPropertyChanged(nameof(CategoryFilterText));
        OnPropertyChanged(nameof(ClubFilterText));
        ClearFiltersCommand.NotifyCanExecuteChanged();
        ReloadFromFirstPage();
    }

    partial void OnBirthYearFromChanged(int? value) => OnBirthYearRangeChanged();
    partial void OnBirthYearToChanged(int? value) => OnBirthYearRangeChanged();

    private void OnBirthYearRangeChanged()
    {
        if (!_filterOptionsInitialized)
            return;
        ClearFiltersCommand.NotifyCanExecuteChanged();
        ReloadFromFirstPage();
    }

    [RelayCommand]
    private void ToggleFiltersPanel() => IsFiltersPanelVisible = !IsFiltersPanelVisible;

    [RelayCommand(CanExecute = nameof(HasActiveFilters))]
    private void ClearFilters()
    {
        ClearFilterOptions(AgeGroupFilterOptions);
        ClearFilterOptions(GenderFilterOptions);
        ClearFilterOptions(CategoryFilterOptions);
        ClearFilterOptions(ClubFilterOptions);
        BirthYearFrom = null;
        BirthYearTo = null;
    }

    private bool HasActiveFilters() =>
        AgeGroupFilterOptions.Any(option => option.IsSelected) ||
        GenderFilterOptions.Any(option => option.IsSelected) ||
        CategoryFilterOptions.Any(option => option.IsSelected) ||
        ClubFilterOptions.Any(option => option.IsSelected) ||
        BirthYearFrom.HasValue ||
        BirthYearTo.HasValue;

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
        var result = _windowFactory.CreateAndShow<AthleteAddEditWindow>(id);
        if (result == true) ReloadAfterMutation();
    }
}
