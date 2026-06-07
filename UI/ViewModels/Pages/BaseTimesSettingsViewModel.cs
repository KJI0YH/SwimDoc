using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.BaseTimeRepository;
using UI.Resources;
using static UI.Models.BaseTimes.BaseTimesSwimStyleCatalog;

namespace UI.ViewModels.Pages;

public sealed partial class BaseTimesSettingsViewModel : ObservableObject
{
    private const string WorldAquaticsPointsUrl = "https://www.worldaquatics.com/swimming/points";
    private readonly IBaseTimeRepository _baseTimeRepository;
    [ObservableProperty] private ObservableCollection<BaseTimeTableRowViewModel> _scmRows = new();
    [ObservableProperty] private ObservableCollection<BaseTimeTableRowViewModel> _lcmRows = new();
    [ObservableProperty] private ObservableCollection<MixedRelayRowViewModel> _scmMixedRelayRows = new();
    [ObservableProperty] private ObservableCollection<MixedRelayRowViewModel> _lcmMixedRelayRows = new();
    public BaseTimesSettingsViewModel(
        IBaseTimeRepository baseTimeRepository,
        ILocalizationService localizationService)
    {
        _baseTimeRepository = baseTimeRepository;
        localizationService.CultureChanged += OnCultureChanged;
        LoadRows();
    }

    private void LoadRows()
    {
        ScmRows = CreateMenWomenRows(Course.SCM, ScmMenWomen);
        LcmRows = CreateMenWomenRows(Course.LCM, LcmMenWomen);
        ScmMixedRelayRows = CreateMixedRelayRows(Course.SCM, ScmMixedRelay);
        LcmMixedRelayRows = CreateMixedRelayRows(Course.LCM, LcmMixedRelay);
    }

    private ObservableCollection<BaseTimeTableRowViewModel> CreateMenWomenRows(
        Course course,
        IReadOnlyList<SwimStyleSpec> specs)
    {
        return new ObservableCollection<BaseTimeTableRowViewModel>(
            specs.Select(spec => CreateMenWomenRow(course, spec)));
    }

    private ObservableCollection<MixedRelayRowViewModel> CreateMixedRelayRows(
        Course course,
        IReadOnlyList<SwimStyleSpec> specs)
    {
        return new ObservableCollection<MixedRelayRowViewModel>(
            specs.Select(spec => CreateMixedRelayRow(course, spec)));
    }

    private BaseTimeTableRowViewModel CreateMenWomenRow(Course course, SwimStyleSpec spec)
    {
        var men = _baseTimeRepository.GetBaseTime(course, spec.Distance, spec.Stroke, spec.RelayCount, Gender.Male);
        var women = _baseTimeRepository.GetBaseTime(course, spec.Distance, spec.Stroke, spec.RelayCount, Gender.Female);
        return new BaseTimeTableRowViewModel(
            course,
            spec.Distance,
            spec.Stroke,
            spec.RelayCount,
            men,
            women);
    }

    private MixedRelayRowViewModel CreateMixedRelayRow(Course course, SwimStyleSpec spec)
    {
        var mixed = _baseTimeRepository.GetBaseTime(course, spec.Distance, spec.Stroke, spec.RelayCount, Gender.Mixed);
        return new MixedRelayRowViewModel(
            course,
            spec.Distance,
            spec.Stroke,
            spec.RelayCount,
            mixed);
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
        foreach (var row in ScmRows)
            row.RefreshDisplayName();
        foreach (var row in LcmRows)
            row.RefreshDisplayName();
        foreach (var row in ScmMixedRelayRows)
            row.RefreshDisplayName();
        foreach (var row in LcmMixedRelayRows)
            row.RefreshDisplayName();
        ScmRows = new ObservableCollection<BaseTimeTableRowViewModel>(ScmRows);
        LcmRows = new ObservableCollection<BaseTimeTableRowViewModel>(LcmRows);
        ScmMixedRelayRows = new ObservableCollection<MixedRelayRowViewModel>(ScmMixedRelayRows);
        LcmMixedRelayRows = new ObservableCollection<MixedRelayRowViewModel>(LcmMixedRelayRows);
    }

    public void ReloadFromRepository() => LoadRows();

    [RelayCommand]
    private static void OpenWorldAquaticsPoints()
    {
        Process.Start(new ProcessStartInfo(WorldAquaticsPointsUrl) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task Save()
    {
        foreach (var row in ScmRows)
        {
            _baseTimeRepository.SetBaseTime(row.Course, row.Distance, row.Stroke, row.RelayCount, Gender.Male, row.MenBaseTimeHundredths ?? 0);
            _baseTimeRepository.SetBaseTime(row.Course, row.Distance, row.Stroke, row.RelayCount, Gender.Female, row.WomenBaseTimeHundredths ?? 0);
        }
        foreach (var row in LcmRows)
        {
            _baseTimeRepository.SetBaseTime(row.Course, row.Distance, row.Stroke, row.RelayCount, Gender.Male, row.MenBaseTimeHundredths ?? 0);
            _baseTimeRepository.SetBaseTime(row.Course, row.Distance, row.Stroke, row.RelayCount, Gender.Female, row.WomenBaseTimeHundredths ?? 0);
        }
        foreach (var row in ScmMixedRelayRows)
            _baseTimeRepository.SetBaseTime(row.Course, row.Distance, row.Stroke, row.RelayCount, Gender.Mixed, row.MixedBaseTimeHundredths ?? 0);
        foreach (var row in LcmMixedRelayRows)
            _baseTimeRepository.SetBaseTime(row.Course, row.Distance, row.Stroke, row.RelayCount, Gender.Mixed, row.MixedBaseTimeHundredths ?? 0);
        try
        {
            _baseTimeRepository.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var dialogs = App.Current.Services.GetRequiredService<IErrorDialogService>();
            await dialogs.ShowErrorAsync(
                title: Strings.Dialog_Error_SaveBaseTimes_Title,
                message: Strings.Dialog_Error_BaseTimesFileBusyOrUnavailable);
        }
    }
}
