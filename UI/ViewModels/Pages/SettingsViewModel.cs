using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using BizLogic.EntryDocumentReader;
using ServiceLayer.EntryDocumentTemplateService;
using ServiceLayer.EntryImportSettings;
using ServiceLayer.Logging;
using ServiceLayer.Scoring;
using UI.Resources;
using UI.Services.FontScale;
using UI.ViewModels;

namespace UI.ViewModels.Pages;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IEntryDocumentTemplateService _entryDocumentTemplateService;
    private readonly ILocalizationService _localizationService;
    private readonly IEntryImportSettingsService _entryImportSettingsService;
    private readonly IFontScaleService _fontScaleService;
    private readonly IScoringSettingsService _scoringSettingsService;
    private bool _suppressScoringHandlers;

    public ObservableCollection<AppLanguage> AvailableLanguages { get; } =
        new([AppLanguage.Russian, AppLanguage.English]);

    public ObservableCollection<PagingSettingItemViewModel> PagingSettings { get; }
    public BaseTimesSettingsViewModel BaseTimes { get; }
    public RankTimesSettingsViewModel RankTimes { get; }
    public ObservableCollection<PlacePointRowViewModel> PlacePointRows { get; } = new();

    [ObservableProperty] private AppLanguage _selectedLanguage;
    [ObservableProperty] private bool _isBaseTimesOpen;
    [ObservableProperty] private bool _isRankTimesOpen;
    [ObservableProperty] private bool _isScoringOpen;
    [ObservableProperty] private bool _isPagingOpen;
    [ObservableProperty] private EntryImportHighlightScoringMode _highlightScoringMode;
    [ObservableProperty] private int _fontSize;
    [ObservableProperty] private ScoringMode _selectedScoringMode;
    [ObservableProperty] private bool _isRecalculating;

    public Array HighlightScoringModes => Enum.GetValues<EntryImportHighlightScoringMode>();
    public Array ScoringModes => Enum.GetValues<ScoringMode>();
    public bool IsPlaceTableMode => SelectedScoringMode == ScoringMode.PlaceTable;
    public bool IsWorldAquaticsMode => SelectedScoringMode == ScoringMode.WorldAquatics;
    public bool IsSettingsHubVisible => !IsScoringOpen && !IsBaseTimesOpen && !IsRankTimesOpen && !IsPagingOpen;

    public SettingsViewModel(
        IEntryDocumentTemplateService entryDocumentTemplateService,
        ILocalizationService localizationService,
        IEntryImportSettingsService entryImportSettingsService,
        IFontScaleService fontScaleService,
        IPagingSettingsService pagingSettingsService,
        BaseTimesSettingsViewModel baseTimesSettingsViewModel,
        RankTimesSettingsViewModel rankTimesSettingsViewModel,
        IScoringSettingsService scoringSettingsService)
    {
        _entryDocumentTemplateService = entryDocumentTemplateService;
        _localizationService = localizationService;
        _entryImportSettingsService = entryImportSettingsService;
        _fontScaleService = fontScaleService;
        _scoringSettingsService = scoringSettingsService;
        BaseTimes = baseTimesSettingsViewModel;
        RankTimes = rankTimesSettingsViewModel;
        _selectedLanguage = localizationService.CurrentLanguage;
        _highlightScoringMode = entryImportSettingsService.HighlightScoringMode;
        _fontSize = fontScaleService.CurrentFontSize;
        PagingSettings = new ObservableCollection<PagingSettingItemViewModel>(
            PagingSettingsService.NavigationOrder
                .Select(page => new PagingSettingItemViewModel(pagingSettingsService, page)));
        _localizationService.CultureChanged += OnCultureChanged;
        _fontScaleService.FontSizeChanged += OnFontScaleChanged;
        LoadScoringFromServices();
    }

    private void LoadScoringFromServices()
    {
        _suppressScoringHandlers = true;
        try
        {
            ApplyActiveSettingsToUi(_scoringSettingsService.Current);
        }
        finally
        {
            _suppressScoringHandlers = false;
        }
    }

    private void ApplyActiveSettingsToUi(ActiveScoringSettings settings)
    {
        SelectedScoringMode = settings.Mode;
        ReplacePlacePointRows(settings.PlacePoints);
        OnPropertyChanged(nameof(IsPlaceTableMode));
        OnPropertyChanged(nameof(IsWorldAquaticsMode));
    }

    private void ReplacePlacePointRows(IReadOnlyList<int> points)
    {
        PlacePointRows.Clear();
        var list = points.Count > 0 ? points : ActiveScoringSettings.DefaultPlacePoints;
        for (var i = 0; i < list.Count; i++)
            PlacePointRows.Add(new PlacePointRowViewModel(i + 1, list[i]));
    }

    partial void OnFontSizeChanged(int value)
    {
        var normalized = _fontScaleService.SetFontSize(value);
        if (normalized != value)
            FontSize = normalized;
    }

    private void OnFontScaleChanged(int fontSize)
    {
        if (fontSize != FontSize)
            FontSize = fontSize;
    }

    partial void OnSelectedLanguageChanged(AppLanguage value) =>
        _localizationService.SetLanguage(value);

    partial void OnHighlightScoringModeChanged(EntryImportHighlightScoringMode value) =>
        _entryImportSettingsService.SetHighlightScoringMode(value);

    partial void OnSelectedScoringModeChanged(ScoringMode value)
    {
        OnPropertyChanged(nameof(IsPlaceTableMode));
        OnPropertyChanged(nameof(IsWorldAquaticsMode));
        if (value == ScoringMode.WorldAquatics)
            BaseTimes.EnsureLoaded();
        if (_suppressScoringHandlers)
            return;
        _ = PersistActiveSettingsAndRecalculateAsync();
    }

    private void OnCultureChanged(CultureInfo _)
    {
        foreach (var item in PagingSettings)
            item.RefreshDisplayText();
        foreach (var row in PlacePointRows)
            row.RefreshDisplayText();
        BaseTimes.RefreshDisplayNames();
        RankTimes.RefreshDisplayNames();
        OnPropertyChanged(nameof(HighlightScoringModes));
        OnPropertyChanged(nameof(ScoringModes));
    }

    [RelayCommand]
    private void OpenScoring()
    {
        LoadScoringFromServices();
        if (IsWorldAquaticsMode)
            BaseTimes.EnsureLoaded();
        IsBaseTimesOpen = false;
        IsRankTimesOpen = false;
        IsPagingOpen = false;
        IsScoringOpen = true;
    }

    [RelayCommand]
    private void CloseScoring()
    {
        PersistActiveSettings();
        IsScoringOpen = false;
        if (IsWorldAquaticsMode)
            BaseTimes.ReloadFromRepository();
    }

    [RelayCommand]
    private void OpenBaseTimes()
    {
        BaseTimes.EnsureLoaded();
        IsScoringOpen = false;
        IsRankTimesOpen = false;
        IsPagingOpen = false;
        IsBaseTimesOpen = true;
    }

    [RelayCommand]
    private void OpenRankTimes()
    {
        RankTimes.EnsureLoaded();
        IsScoringOpen = false;
        IsBaseTimesOpen = false;
        IsPagingOpen = false;
        IsRankTimesOpen = true;
    }

    [RelayCommand]
    private void OpenPaging()
    {
        IsScoringOpen = false;
        IsBaseTimesOpen = false;
        IsRankTimesOpen = false;
        IsPagingOpen = true;
    }

    [RelayCommand]
    private void ClosePaging()
    {
        IsPagingOpen = false;
    }

    [RelayCommand]
    private void CloseBaseTimes()
    {
        IsBaseTimesOpen = false;
        BaseTimes.ReloadFromRepository();
    }

    [RelayCommand]
    private void CloseRankTimes()
    {
        IsRankTimesOpen = false;
        RankTimes.ReloadFromRepository();
    }

    partial void OnIsScoringOpenChanged(bool value) => OnPropertyChanged(nameof(IsSettingsHubVisible));
    partial void OnIsBaseTimesOpenChanged(bool value) => OnPropertyChanged(nameof(IsSettingsHubVisible));
    partial void OnIsRankTimesOpenChanged(bool value) => OnPropertyChanged(nameof(IsSettingsHubVisible));
    partial void OnIsPagingOpenChanged(bool value) => OnPropertyChanged(nameof(IsSettingsHubVisible));

    [RelayCommand]
    private async Task DownloadEntryDocumentTemplate()
    {
        var dialog = new SaveFileDialog
        {
            Title = Strings.Dialog_SaveExcelTemplate_Title,
            Filter = Strings.Dialog_SaveExcelTemplate_Filter,
            FileName = Strings.Dialog_SaveExcelTemplate_DefaultFileName,
            AddExtension = true,
            DefaultExt = ".xlsx",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true)
            return;
        try
        {
            var bytes = _entryDocumentTemplateService.CreateTemplate();
            File.WriteAllBytes(dialog.FileName, bytes);
            App.Current.Services.GetRequiredService<IAppLog>().Info($"Saved entry document template: \"{dialog.FileName}\"");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var dialogs = App.Current.Services.GetRequiredService<IErrorDialogService>();
            await dialogs.ShowErrorAsync(
                title: Strings.Dialog_Error_SaveFile_Title,
                message: Strings.Dialog_Error_FileBusyOrUnavailable);
        }
    }

    [RelayCommand]
    private void AddPlacePointRow()
    {
        PlacePointRows.Add(new PlacePointRowViewModel(PlacePointRows.Count + 1, 0));
    }

    [RelayCommand]
    private void RemoveLastPlacePointRow()
    {
        if (PlacePointRows.Count <= 1)
            return;
        PlacePointRows.RemoveAt(PlacePointRows.Count - 1);
    }

    [RelayCommand]
    private void ResetPlacePointsToDefault()
    {
        ReplacePlacePointRows(ActiveScoringSettings.DefaultPlacePoints);
    }

    [RelayCommand]
    private async Task ApplyPlaceTable()
    {
        await PersistActiveSettingsAndRecalculateAsync();
    }

    private void PersistActiveSettings()
    {
        _scoringSettingsService.SetActive(new ActiveScoringSettings
        {
            Mode = SelectedScoringMode,
            PlacePoints = PlacePointRows.Select(r => r.Points).ToList()
        });
    }

    private async Task PersistActiveSettingsAndRecalculateAsync()
    {
        PersistActiveSettings();
        await RecalculatePointsAsync();
    }

    private async Task RecalculatePointsAsync()
    {
        IsRecalculating = true;
        try
        {
            var recalc = App.Current.Services.GetRequiredService<IPointsRecalculationService>();
            await Task.Run(async () => await recalc.RecalculateAllAsync().ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            App.Current.Services.GetRequiredService<IAppLog>().Warning($"Points recalculation failed: {ex.Message}");
            var dialogs = App.Current.Services.GetRequiredService<IErrorDialogService>();
            await dialogs.ShowErrorAsync(
                title: Strings.Settings_Scoring_RecalcError_Title,
                message: Strings.Settings_Scoring_RecalcError_Message);
        }
        finally
        {
            IsRecalculating = false;
        }
    }

    private void RenumberPlaceRows()
    {
        for (var i = 0; i < PlacePointRows.Count; i++)
        {
            var current = PlacePointRows[i];
            if (current.Place == i + 1)
                continue;
            PlacePointRows[i] = new PlacePointRowViewModel(i + 1, current.Points);
        }
    }
}
