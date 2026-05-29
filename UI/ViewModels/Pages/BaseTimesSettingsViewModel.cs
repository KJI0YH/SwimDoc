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
using UI.Helpers;
using UI.Resources;
using UI.Services;
using static UI.ViewModels.Pages.BaseTimesSwimStyleCatalog;

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
            _baseTimeRepository,
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
            _baseTimeRepository,
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

        // DataGrid caches read-only cells while collapsed; reassign collections to force rebind.
        ScmRows = new ObservableCollection<BaseTimeTableRowViewModel>(ScmRows);
        LcmRows = new ObservableCollection<BaseTimeTableRowViewModel>(LcmRows);
        ScmMixedRelayRows = new ObservableCollection<MixedRelayRowViewModel>(ScmMixedRelayRows);
        LcmMixedRelayRows = new ObservableCollection<MixedRelayRowViewModel>(LcmMixedRelayRows);
    }

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

public sealed partial class BaseTimeTableRowViewModel : ObservableObject
{
    private readonly IBaseTimeRepository _repository;
    private bool _suppressSync;

    public Course Course { get; }
    public int Distance { get; }
    public Stroke Stroke { get; }
    public int RelayCount { get; }

    [ObservableProperty] private string _name;

    public void RefreshDisplayName() => Name = FormatDisplayName();

    private SwimStyle ToSwimStyle() => new()
    {
        RelayCount = RelayCount,
        Distance = Distance,
        Stroke = Stroke
    };

    private string FormatDisplayName() => EntityDisplayFormatter.FormatSwimStyle(ToSwimStyle());

    [ObservableProperty] private string _menBaseTimeText;
    [ObservableProperty] private string _womenBaseTimeText;
    [ObservableProperty] private string _menSecondsText;
    [ObservableProperty] private string _womenSecondsText;
    [ObservableProperty] private int? _menBaseTimeHundredths;
    [ObservableProperty] private int? _womenBaseTimeHundredths;

    public BaseTimeTableRowViewModel(
        IBaseTimeRepository repository,
        Course course,
        int distance,
        Stroke stroke,
        int relayCount,
        int menHundredthsFromStore,
        int womenHundredthsFromStore)
    {
        _repository = repository;
        Course = course;
        Distance = distance;
        Stroke = stroke;
        RelayCount = relayCount;
        _name = FormatDisplayName();

        MenBaseTimeHundredths = menHundredthsFromStore;
        WomenBaseTimeHundredths = womenHundredthsFromStore;
        _menBaseTimeText = BaseTimeHundredthsText.FormatClock(MenBaseTimeHundredths);
        _womenBaseTimeText = BaseTimeHundredthsText.FormatClock(WomenBaseTimeHundredths);
        _menSecondsText = BaseTimeHundredthsText.FormatSecondsField(MenBaseTimeHundredths);
        _womenSecondsText = BaseTimeHundredthsText.FormatSecondsField(WomenBaseTimeHundredths);
    }

    partial void OnMenBaseTimeTextChanged(string value)
    {
        if (_suppressSync) return;
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        var parsed = BaseTimeHundredthsText.ParseFromMmSsHhDigits(digits);
        MenBaseTimeHundredths = parsed;
        _suppressSync = true;
        try { MenSecondsText = BaseTimeHundredthsText.FormatSecondsField(parsed); }
        finally { _suppressSync = false; }
        _repository.SetBaseTime(Course, Distance, Stroke, RelayCount, Gender.Male, parsed ?? 0);
    }

    partial void OnWomenBaseTimeTextChanged(string value)
    {
        if (_suppressSync) return;
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        var parsed = BaseTimeHundredthsText.ParseFromMmSsHhDigits(digits);
        WomenBaseTimeHundredths = parsed;
        _suppressSync = true;
        try { WomenSecondsText = BaseTimeHundredthsText.FormatSecondsField(parsed); }
        finally { _suppressSync = false; }
        _repository.SetBaseTime(Course, Distance, Stroke, RelayCount, Gender.Female, parsed ?? 0);
    }

    partial void OnMenSecondsTextChanged(string value)
    {
        if (_suppressSync) return;
        var parsed = BaseTimeHundredthsText.ParseSecondsField(value);
        MenBaseTimeHundredths = parsed;
        _suppressSync = true;
        try
        {
            MenSecondsText = BaseTimeHundredthsText.FormatSecondsField(parsed);
            MenBaseTimeText = BaseTimeHundredthsText.FormatClock(parsed);
        }
        finally { _suppressSync = false; }
        _repository.SetBaseTime(Course, Distance, Stroke, RelayCount, Gender.Male, parsed ?? 0);
    }

    partial void OnWomenSecondsTextChanged(string value)
    {
        if (_suppressSync) return;
        var parsed = BaseTimeHundredthsText.ParseSecondsField(value);
        WomenBaseTimeHundredths = parsed;
        _suppressSync = true;
        try
        {
            WomenSecondsText = BaseTimeHundredthsText.FormatSecondsField(parsed);
            WomenBaseTimeText = BaseTimeHundredthsText.FormatClock(parsed);
        }
        finally { _suppressSync = false; }
        _repository.SetBaseTime(Course, Distance, Stroke, RelayCount, Gender.Female, parsed ?? 0);
    }

    partial void OnMenBaseTimeHundredthsChanged(int? value)
    {
        var formatted = BaseTimeHundredthsText.FormatClock(value);
        if (!string.Equals(_menBaseTimeText, formatted, StringComparison.Ordinal))
        {
            _menBaseTimeText = formatted;
            OnPropertyChanged(nameof(MenBaseTimeText));
        }
    }

    partial void OnWomenBaseTimeHundredthsChanged(int? value)
    {
        var formatted = BaseTimeHundredthsText.FormatClock(value);
        if (!string.Equals(_womenBaseTimeText, formatted, StringComparison.Ordinal))
        {
            _womenBaseTimeText = formatted;
            OnPropertyChanged(nameof(WomenBaseTimeText));
        }
    }
}

public sealed partial class MixedRelayRowViewModel : ObservableObject
{
    private readonly IBaseTimeRepository _repository;
    private bool _suppressSync;

    public Course Course { get; }
    public int Distance { get; }
    public Stroke Stroke { get; }
    public int RelayCount { get; }

    [ObservableProperty] private string _name;

    public void RefreshDisplayName() => Name = FormatDisplayName();

    private SwimStyle ToSwimStyle() => new()
    {
        RelayCount = RelayCount,
        Distance = Distance,
        Stroke = Stroke
    };

    private string FormatDisplayName() => EntityDisplayFormatter.FormatSwimStyle(ToSwimStyle());

    [ObservableProperty] private string _mixedBaseTimeText;
    [ObservableProperty] private string _mixedSecondsText;
    [ObservableProperty] private int? _mixedBaseTimeHundredths;

    public MixedRelayRowViewModel(
        IBaseTimeRepository repository,
        Course course,
        int distance,
        Stroke stroke,
        int relayCount,
        int mixedHundredthsFromStore)
    {
        _repository = repository;
        Course = course;
        Distance = distance;
        Stroke = stroke;
        RelayCount = relayCount;
        _name = FormatDisplayName();

        MixedBaseTimeHundredths = mixedHundredthsFromStore;
        _mixedBaseTimeText = BaseTimeHundredthsText.FormatClock(MixedBaseTimeHundredths);
        _mixedSecondsText = BaseTimeHundredthsText.FormatSecondsField(MixedBaseTimeHundredths);
    }

    partial void OnMixedBaseTimeTextChanged(string value)
    {
        if (_suppressSync) return;
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        var parsed = BaseTimeHundredthsText.ParseFromMmSsHhDigits(digits);
        MixedBaseTimeHundredths = parsed;
        _suppressSync = true;
        try { MixedSecondsText = BaseTimeHundredthsText.FormatSecondsField(parsed); }
        finally { _suppressSync = false; }
        _repository.SetBaseTime(Course, Distance, Stroke, RelayCount, Gender.Mixed, parsed ?? 0);
    }

    partial void OnMixedSecondsTextChanged(string value)
    {
        if (_suppressSync) return;
        var parsed = BaseTimeHundredthsText.ParseSecondsField(value);
        MixedBaseTimeHundredths = parsed;
        _suppressSync = true;
        try
        {
            MixedSecondsText = BaseTimeHundredthsText.FormatSecondsField(parsed);
            MixedBaseTimeText = BaseTimeHundredthsText.FormatClock(parsed);
        }
        finally { _suppressSync = false; }
        _repository.SetBaseTime(Course, Distance, Stroke, RelayCount, Gender.Mixed, parsed ?? 0);
    }

    partial void OnMixedBaseTimeHundredthsChanged(int? value)
    {
        var formatted = BaseTimeHundredthsText.FormatClock(value);
        if (!string.Equals(_mixedBaseTimeText, formatted, StringComparison.Ordinal))
        {
            _mixedBaseTimeText = formatted;
            OnPropertyChanged(nameof(MixedBaseTimeText));
        }
    }
}

file static class BaseTimeHundredthsText
{
    public static int? ParseFromMmSsHhDigits(string digits)
    {
        if (string.IsNullOrWhiteSpace(digits))
            return null;

        var normalized = digits.TrimStart('0');
        if (normalized.Length == 0)
            normalized = "0";
        if (normalized.Length > 9)
            normalized = normalized[^9..];

        var padded = normalized.PadLeft(4, '0');
        var hundredthsPart = padded[^2..];
        var secondsPart = padded[^4..^2];
        var minutesPart = padded.Length > 4 ? padded[..^4] : "0";

        if (!int.TryParse(minutesPart, out var minutes))
            minutes = 0;
        if (!int.TryParse(secondsPart, out var seconds))
            seconds = 0;
        if (!int.TryParse(hundredthsPart, out var hundredths))
            hundredths = 0;

        return minutes * 6000 + seconds * 100 + hundredths;
    }

    public static string FormatClock(int? value)
    {
        if (!value.HasValue)
            return string.Empty;

        var totalHundredths = value.Value;
        if (totalHundredths < 0)
            totalHundredths = 0;

        var minutes = totalHundredths / 6000;
        var seconds = totalHundredths % 6000 / 100;
        var hundredths = totalHundredths % 100;

        return minutes > 0
            ? $"{minutes}:{seconds:D2}.{hundredths:D2}"
            : $"{seconds}.{hundredths:D2}";
    }

    public static string FormatSecondsField(int? value)
    {
        if (!value.HasValue)
            return string.Empty;

        var hundredths = value.Value;
        if (hundredths <= 0)
            return string.Empty;
        return (hundredths / 100d).ToString("0.00", CultureInfo.InvariantCulture);
    }

    public static int? ParseSecondsField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Trim().Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;

        var normalized = digits.TrimStart('0');
        if (normalized.Length == 0)
            normalized = "0";
        if (normalized.Length > 9)
            normalized = normalized[^9..];

        normalized = normalized.PadLeft(3, '0');
        var hundredthsPart = normalized[^2..];
        var secondsPart = normalized[..^2];

        if (!int.TryParse(secondsPart, out var seconds))
            seconds = 0;
        if (!int.TryParse(hundredthsPart, out var hundredths))
            hundredths = 0;

        if (seconds <= 0 && hundredths <= 0)
            return null;

        return seconds * 100 + hundredths;
    }
}
