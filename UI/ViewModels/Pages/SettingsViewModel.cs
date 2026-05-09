using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using ServiceLayer.BaseTimeRepository;

namespace UI.ViewModels.Pages;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IBaseTimeRepository _baseTimeRepository;

    [ObservableProperty] private ObservableCollection<BaseTimeTableRowViewModel> _scmRows = new();
    [ObservableProperty] private ObservableCollection<BaseTimeTableRowViewModel> _lcmRows = new();
    [ObservableProperty] private ObservableCollection<MixedRelayRowViewModel> _scmMixedRelayRows = new();
    [ObservableProperty] private ObservableCollection<MixedRelayRowViewModel> _lcmMixedRelayRows = new();

    private readonly record struct MenWomenTableRow(
        Course Course,
        int RelayCount,
        int Distance,
        Stroke Stroke,
        string DisplayName);

    private readonly record struct MixedRelayTableRow(
        Course Course,
        int RelayCount,
        int Distance,
        Stroke Stroke,
        string DisplayName);

    private static readonly MenWomenTableRow[] ScmMenWomenRows =
    [
        new(Course.SCM, 0, 50, Stroke.Free, "50м Вольный стиль"),
        new(Course.SCM, 0, 100, Stroke.Free, "100м Вольный стиль"),
        new(Course.SCM, 0, 200, Stroke.Free, "200м Вольный стиль"),
        new(Course.SCM, 0, 400, Stroke.Free, "400м Вольный стиль"),
        new(Course.SCM, 0, 800, Stroke.Free, "800м Вольный стиль"),
        new(Course.SCM, 0, 1500, Stroke.Free, "1500м Вольный стиль"),
        new(Course.SCM, 0, 50, Stroke.Back, "50м На спине"),
        new(Course.SCM, 0, 100, Stroke.Back, "100м На спине"),
        new(Course.SCM, 0, 200, Stroke.Back, "200м На спине"),
        new(Course.SCM, 0, 50, Stroke.Breast, "50м Брасс"),
        new(Course.SCM, 0, 100, Stroke.Breast, "100м Брасс"),
        new(Course.SCM, 0, 200, Stroke.Breast, "200м Брасс"),
        new(Course.SCM, 0, 50, Stroke.Fly, "50м Баттерфляй"),
        new(Course.SCM, 0, 100, Stroke.Fly, "100м Баттерфляй"),
        new(Course.SCM, 0, 200, Stroke.Fly, "200м Баттерфляй"),
        new(Course.SCM, 0, 100, Stroke.Medley, "100м Комплексное плавание"),
        new(Course.SCM, 0, 200, Stroke.Medley, "200м Комплексное плавание"),
        new(Course.SCM, 0, 400, Stroke.Medley, "400м Комплексное плавание"),
        new(Course.SCM, 4, 50, Stroke.Free, "4x50м Вольный стиль"),
        new(Course.SCM, 4, 100, Stroke.Free, "4x100м Вольный стиль"),
        new(Course.SCM, 4, 200, Stroke.Free, "4x200м Вольный стиль"),
        new(Course.SCM, 4, 50, Stroke.Medley, "4x50м Комплексное плавание"),
        new(Course.SCM, 4, 100, Stroke.Medley, "4x100м Комплексное плавание"),
    ];

    private static readonly MenWomenTableRow[] LcmMenWomenRows =
    [
        new(Course.LCM, 0, 50, Stroke.Free, "50м Вольный стиль"),
        new(Course.LCM, 0, 100, Stroke.Free, "100м Вольный стиль"),
        new(Course.LCM, 0, 200, Stroke.Free, "200м Вольный стиль"),
        new(Course.LCM, 0, 400, Stroke.Free, "400м Вольный стиль"),
        new(Course.LCM, 0, 800, Stroke.Free, "800м Вольный стиль"),
        new(Course.LCM, 0, 1500, Stroke.Free, "1500м Вольный стиль"),
        new(Course.LCM, 0, 50, Stroke.Back, "50м На спине"),
        new(Course.LCM, 0, 100, Stroke.Back, "100м На спине"),
        new(Course.LCM, 0, 200, Stroke.Back, "200м На спине"),
        new(Course.LCM, 0, 50, Stroke.Breast, "50м Брасс"),
        new(Course.LCM, 0, 100, Stroke.Breast, "100м Брасс"),
        new(Course.LCM, 0, 200, Stroke.Breast, "200м Брасс"),
        new(Course.LCM, 0, 50, Stroke.Fly, "50м Баттерфляй"),
        new(Course.LCM, 0, 100, Stroke.Fly, "100м Баттерфляй"),
        new(Course.LCM, 0, 200, Stroke.Fly, "200м Баттерфляй"),
        new(Course.LCM, 0, 200, Stroke.Medley, "200м Комплексное плавание"),
        new(Course.LCM, 0, 400, Stroke.Medley, "400м Комплексное плавание"),
        new(Course.LCM, 4, 100, Stroke.Free, "4x100м Вольный стиль"),
        new(Course.LCM, 4, 200, Stroke.Free, "4x200м Вольный стиль"),
        new(Course.LCM, 4, 100, Stroke.Medley, "4x100м Комплексное плавание"),
    ];

    private static readonly MixedRelayTableRow[] ScmMixedRelayTable =
    [
        new(Course.SCM, 4, 50, Stroke.Free, "4x50м Вольный стиль (Микст)"),
        new(Course.SCM, 4, 50, Stroke.Medley, "4x50м Комплексное плавание (Микст)"),
    ];

    private static readonly MixedRelayTableRow[] LcmMixedRelayTable =
    [
        new(Course.LCM, 4, 100, Stroke.Free, "4x100м Вольный стиль (Микст)"),
        new(Course.LCM, 4, 100, Stroke.Medley, "4x100м Комплексное плавание (Микст)"),
    ];

    public SettingsViewModel(IBaseTimeRepository baseTimeRepository)
    {
        _baseTimeRepository = baseTimeRepository;
        LoadRows();
    }

    private void LoadRows()
    {
        ScmRows = new ObservableCollection<BaseTimeTableRowViewModel>(ScmMenWomenRows.Select(CreateMenWomenRow));
        LcmRows = new ObservableCollection<BaseTimeTableRowViewModel>(LcmMenWomenRows.Select(CreateMenWomenRow));
        ScmMixedRelayRows = new ObservableCollection<MixedRelayRowViewModel>(ScmMixedRelayTable.Select(CreateMixedRelayRow));
        LcmMixedRelayRows = new ObservableCollection<MixedRelayRowViewModel>(LcmMixedRelayTable.Select(CreateMixedRelayRow));
    }

    private BaseTimeTableRowViewModel CreateMenWomenRow(MenWomenTableRow row)
    {
        var men = _baseTimeRepository.GetBaseTime(row.Course, row.Distance, row.Stroke, row.RelayCount, Gender.Male);
        var women = _baseTimeRepository.GetBaseTime(row.Course, row.Distance, row.Stroke, row.RelayCount, Gender.Female);
        return new BaseTimeTableRowViewModel(_baseTimeRepository, row.Course, row.Distance, row.Stroke, row.RelayCount, row.DisplayName, men, women);
    }

    private MixedRelayRowViewModel CreateMixedRelayRow(MixedRelayTableRow row)
    {
        var mixed = _baseTimeRepository.GetBaseTime(row.Course, row.Distance, row.Stroke, row.RelayCount, Gender.Mixed);
        return new MixedRelayRowViewModel(_baseTimeRepository, row.Course, row.Distance, row.Stroke, row.RelayCount, row.DisplayName, mixed);
    }

    [RelayCommand]
    private void Save()
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

        _baseTimeRepository.Save();
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
    public string Name { get; }

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
        string name,
        int menHundredthsFromStore,
        int womenHundredthsFromStore)
    {
        _repository = repository;
        Course = course;
        Distance = distance;
        Stroke = stroke;
        RelayCount = relayCount;
        Name = name;

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
    public string Name { get; }

    [ObservableProperty] private string _mixedBaseTimeText;
    [ObservableProperty] private string _mixedSecondsText;
    [ObservableProperty] private int? _mixedBaseTimeHundredths;

    public MixedRelayRowViewModel(
        IBaseTimeRepository repository,
        Course course,
        int distance,
        Stroke stroke,
        int relayCount,
        string name,
        int mixedHundredthsFromStore)
    {
        _repository = repository;
        Course = course;
        Distance = distance;
        Stroke = stroke;
        RelayCount = relayCount;
        Name = name;

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
