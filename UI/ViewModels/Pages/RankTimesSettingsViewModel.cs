using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.Display;
using DataLayer.EfClasses;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.RankTimeRepository;
using UI.Resources;
using static UI.Models.BaseTimes.BaseTimesSwimStyleCatalog;

namespace UI.ViewModels.Pages;

public sealed partial class RankTimesSettingsViewModel : ObservableObject
{
    private readonly IRankTimeRepository _rankTimeRepository;
    private bool _rowsLoaded;
    [ObservableProperty] private ObservableCollection<RankTimeTableRowViewModel> _scmMenRows = new();
    [ObservableProperty] private ObservableCollection<RankTimeTableRowViewModel> _scmWomenRows = new();
    [ObservableProperty] private ObservableCollection<RankTimeTableRowViewModel> _lcmMenRows = new();
    [ObservableProperty] private ObservableCollection<RankTimeTableRowViewModel> _lcmWomenRows = new();

    public RankTimesSettingsViewModel(
        IRankTimeRepository rankTimeRepository,
        ILocalizationService localizationService)
    {
        _rankTimeRepository = rankTimeRepository;
        localizationService.CultureChanged += OnCultureChanged;
    }

    public void EnsureLoaded()
    {
        if (_rowsLoaded)
            return;
        LoadRows();
    }

    private void LoadRows()
    {
        ScmMenRows = CreateRows(Course.SCM, Gender.Male, ScmMenWomen);
        ScmWomenRows = CreateRows(Course.SCM, Gender.Female, ScmMenWomen);
        LcmMenRows = CreateRows(Course.LCM, Gender.Male, LcmMenWomen);
        LcmWomenRows = CreateRows(Course.LCM, Gender.Female, LcmMenWomen);
        _rowsLoaded = true;
    }

    private ObservableCollection<RankTimeTableRowViewModel> CreateRows(
        Course course,
        Gender gender,
        IReadOnlyList<SwimStyleSpec> specs)
    {
        return new ObservableCollection<RankTimeTableRowViewModel>(
            specs.Where(spec => spec.RelayCount == 0)
                .Select(spec => CreateRow(course, gender, spec)));
    }

    private RankTimeTableRowViewModel CreateRow(Course course, Gender gender, SwimStyleSpec spec)
    {
        var times = _rankTimeRepository.GetRankTimes(course, spec.Distance, spec.Stroke, 0, gender);
        return new RankTimeTableRowViewModel(course, gender, spec.Distance, spec.Stroke, times);
    }

    private void OnCultureChanged(CultureInfo _)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(RefreshDisplayNames);
            return;
        }
        RefreshDisplayNames();
    }

    public void RefreshDisplayNames()
    {
        if (!_rowsLoaded)
            return;
        foreach (var row in AllRows())
            row.RefreshDisplayName();
    }

    public void ReloadFromRepository() => LoadRows();

    [RelayCommand]
    private async Task Save()
    {
        PersistRows(AllRows());
        try
        {
            _rankTimeRepository.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var dialogs = App.Current.Services.GetRequiredService<IErrorDialogService>();
            await dialogs.ShowErrorAsync(
                title: Strings.Dialog_Error_SaveRankTimes_Title,
                message: Strings.Dialog_Error_RankTimesFileBusyOrUnavailable);
        }
    }

    private IEnumerable<RankTimeTableRowViewModel> AllRows() =>
        ScmMenRows.Concat(ScmWomenRows).Concat(LcmMenRows).Concat(LcmWomenRows);

    private void PersistRows(IEnumerable<RankTimeTableRowViewModel> rows)
    {
        foreach (var row in rows)
        {
            foreach (var category in CategoryDisplay.FromHighest)
            {
                _rankTimeRepository.SetRankTime(
                    row.Course,
                    row.Distance,
                    row.Stroke,
                    0,
                    row.Gender,
                    category,
                    row.GetHundredths(category));
            }
        }
    }
}
