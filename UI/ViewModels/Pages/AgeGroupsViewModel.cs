using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.ReportGeneratorService;
using UI.Resources;
using UI.ViewModels.Pages.Data;
using UI.Models.Rows;
using UI.Models.Rows.Projections;
using UI.ViewModels.Dialogs.CombinedResultsReportGeneration;
using UI.Views.Dialogs.Markers.AddEdit;
using CombinedResultsReportGenerationWindow = UI.Views.Dialogs.Markers.CombinedResultsReportGeneration.CombinedResultsReportGenerationWindow;

namespace UI.ViewModels.Pages;

public partial class AgeGroupsViewModel : DataViewModel<AgeGroup, AgeGroupRowView, int?>
{
    protected override PagingPage PagingSettingsPage => PagingPage.AgeGroups;
    private readonly IAddEditWindowFactory _windowFactory;
    public AgeGroupsViewModel(IAgeGroupService ageGroupService, EfCoreContext dbContext) : base(ageGroupService)
    {
        _ = dbContext;
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedItems))
            GenerateCombinedResultsReportsCommand.NotifyCanExecuteChanged();
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("DisplayName", Strings.AgeGroups_Col_Name, 400,
            ColumnConfiguration<AgeGroup>.SortBy(ag => ag.Name)));
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("Gender", Strings.AgeGroups_Col_Gender, 100,
            ColumnConfiguration<AgeGroup>.SortBy(ag => ag.Gender)));
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("BirthYearMin", Strings.AgeGroups_Col_BirthYearFrom, 150,
            ColumnConfiguration<AgeGroup>.SortBy(ag => ag.BirthYearMin))
        {
            Converter = new BirthYearBoundConverter(),
            ConverterParameter = BirthYearBoundConverter.Min
        });
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("BirthYearMax", Strings.AgeGroups_Col_BirthYearTo, 150,
            ColumnConfiguration<AgeGroup>.SortBy(ag => ag.BirthYearMax))
        {
            Converter = new BirthYearBoundConverter(),
            ConverterParameter = BirthYearBoundConverter.Max
        });
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("ParticipantCount", Strings.AgeGroups_Col_Participants, 120,
            ColumnConfiguration<AgeGroup>.SortBy(ag => ag.Id)));
    }

    protected override IQueryable<AgeGroup> ApplyQuery(IQueryable<AgeGroup> query) => query;

    protected override IQueryable<AgeGroup> ApplySorting(IQueryable<AgeGroup> query)
    {
        if (!string.Equals(SortColumn, "ParticipantCount", StringComparison.Ordinal))
            return base.ApplySorting(query);

        var db = (LoadServiceProvider ?? App.Current.Services).GetRequiredService<EfCoreContext>();
        var athletes = db.Set<Athlete>().AsNoTracking();
        return SortDirection == ListSortDirection.Ascending
            ? query.OrderBy(ag => athletes.Count(a =>
                a.YearOfBirth >= (ag.BirthYearMin ?? 0) &&
                a.YearOfBirth <= (ag.BirthYearMax ?? int.MaxValue) &&
                (ag.Gender == Gender.Mixed || a.Gender == ag.Gender)))
            : query.OrderByDescending(ag => athletes.Count(a =>
                a.YearOfBirth >= (ag.BirthYearMin ?? 0) &&
                a.YearOfBirth <= (ag.BirthYearMax ?? int.MaxValue) &&
                (ag.Gender == Gender.Mixed || a.Gender == ag.Gender)));
    }

    protected override async Task<List<AgeGroupRowView>> LoadPageRowsAsync(
        IQueryable<AgeGroup> query,
        IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<EfCoreContext>();
        var athletes = db.Set<Athlete>().AsNoTracking();
        var projections = await RowProjectionQueries.SelectAgeGroup(query, athletes).ToListAsync().ConfigureAwait(false);
        return projections.Select(AgeGroupRowView.FromProjection).ToList();
    }

    protected override IQueryable<AgeGroup> ApplySearch(IQueryable<AgeGroup> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;
        if (Strings.TryFindEnumByDisplayContains(SearchText, out Gender gender))
            return query.Where(ag => ag.Gender == gender);
        if (int.TryParse((string?)SearchText, out var year))
            return Queryable.Where(query, ag =>
                EF.Functions.Like(ag.BirthYearMin.ToString(), $"%{SearchText}%") ||
                EF.Functions.Like(ag.BirthYearMax.ToString(), $"%{SearchText}%"));
        var term = SearchText.Trim();
        return Queryable.Where(query, ag =>
            SwimDocDbFunctions.ContainsIgnoreCase(ag.Name, term));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<AgeGroupAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGenerateCombinedResultsReports))]
    private async Task GenerateCombinedResultsReports()
    {
        if (SelectedItems.Count == 0)
            return;
        var dialog = _windowFactory.CreateAndShowAndReturn<CombinedResultsReportGenerationWindow>();
        if (dialog.DataContext is not IWindowResult { Result: CombinedResultsReportGenerationResult result })
            return;
        var options = new CombinedResultsExportOptions
        {
            AgeGroupIds = SelectedItems.Select(ageGroup => ageGroup.Id).ToList(),
            OutputFilePath = result.OutputFilePath
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
                    var tempOptions = new CombinedResultsExportOptions
                    {
                        AgeGroupIds = options.AgeGroupIds,
                        OutputFilePath = tempPath
                    };
                    using var scope = App.Current.Services.CreateScope();
                    scope.ServiceProvider.GetRequiredService<IReportExportService>()
                        .ExportCombinedResultsToExcel(tempOptions);
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

    private bool CanGenerateCombinedResultsReports() => SelectedItems.Count > 0 && !IsOperationRunning;
}
