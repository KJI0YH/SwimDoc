using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer.EfClasses;

namespace UI.ViewModels.Pages;

public sealed partial class RankTimeTableRowViewModel : ObservableObject
{
    public Course Course { get; }
    public Gender Gender { get; }
    public int Distance { get; }
    public Stroke Stroke { get; }
    public int RelayCount => 0;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _msmkText;
    [ObservableProperty] private string _msText;
    [ObservableProperty] private string _kmsText;
    [ObservableProperty] private string _iText;
    [ObservableProperty] private string _iiText;
    [ObservableProperty] private string _iiiText;
    [ObservableProperty] private string _iYunText;
    [ObservableProperty] private string _iiYunText;

    [ObservableProperty] private int? _msmkHundredths;
    [ObservableProperty] private int? _msHundredths;
    [ObservableProperty] private int? _kmsHundredths;
    [ObservableProperty] private int? _iHundredths;
    [ObservableProperty] private int? _iiHundredths;
    [ObservableProperty] private int? _iiiHundredths;
    [ObservableProperty] private int? _iYunHundredths;
    [ObservableProperty] private int? _iiYunHundredths;

    private string _msmkDigits = string.Empty;
    private string _msDigits = string.Empty;
    private string _kmsDigits = string.Empty;
    private string _iDigits = string.Empty;
    private string _iiDigits = string.Empty;
    private string _iiiDigits = string.Empty;
    private string _iYunDigits = string.Empty;
    private string _iiYunDigits = string.Empty;
    private string _msmkDisplay = string.Empty;
    private string _msDisplay = string.Empty;
    private string _kmsDisplay = string.Empty;
    private string _iDisplay = string.Empty;
    private string _iiDisplay = string.Empty;
    private string _iiiDisplay = string.Empty;
    private string _iYunDisplay = string.Empty;
    private string _iiYunDisplay = string.Empty;

    public RankTimeTableRowViewModel(
        Course course,
        Gender gender,
        int distance,
        Stroke stroke,
        IReadOnlyDictionary<Category, int> times)
    {
        Course = course;
        Gender = gender;
        Distance = distance;
        Stroke = stroke;
        _name = FormatDisplayName();
        MsmkHundredths = GetTime(times, Category.IMoS);
        MsHundredths = GetTime(times, Category.MoS);
        KmsHundredths = GetTime(times, Category.CMoS);
        IHundredths = GetTime(times, Category.FirstAdult);
        IiHundredths = GetTime(times, Category.SecondAdult);
        IiiHundredths = GetTime(times, Category.ThirdAdult);
        IYunHundredths = GetTime(times, Category.FirstJunior);
        IiYunHundredths = GetTime(times, Category.SecondJunior);
        InitField(ref _msmkText, ref _msmkDigits, ref _msmkDisplay, MsmkHundredths);
        InitField(ref _msText, ref _msDigits, ref _msDisplay, MsHundredths);
        InitField(ref _kmsText, ref _kmsDigits, ref _kmsDisplay, KmsHundredths);
        InitField(ref _iText, ref _iDigits, ref _iDisplay, IHundredths);
        InitField(ref _iiText, ref _iiDigits, ref _iiDisplay, IiHundredths);
        InitField(ref _iiiText, ref _iiiDigits, ref _iiiDisplay, IiiHundredths);
        InitField(ref _iYunText, ref _iYunDigits, ref _iYunDisplay, IYunHundredths);
        InitField(ref _iiYunText, ref _iiYunDigits, ref _iiYunDisplay, IiYunHundredths);
    }

    public void RefreshDisplayName() => Name = FormatDisplayName();

    private SwimStyle ToSwimStyle() => new()
    {
        RelayCount = 0,
        Distance = Distance,
        Stroke = Stroke
    };

    private string FormatDisplayName() => EntityDisplayFormatter.FormatSwimStyle(ToSwimStyle());

    private static int? GetTime(IReadOnlyDictionary<Category, int> times, Category category) =>
        times.TryGetValue(category, out var value) && value > 0 ? value : null;

    private static void InitField(ref string text, ref string digits, ref string display, int? hundredths)
    {
        text = SwimTimeInput.Format(hundredths);
        digits = SwimTimeInput.ToDigitBuffer(hundredths);
        display = text;
    }

    partial void OnMsmkTextChanged(string value) => ApplyClock(value, ref _msmkDigits, ref _msmkDisplay, v => MsmkHundredths = v);
    partial void OnMsTextChanged(string value) => ApplyClock(value, ref _msDigits, ref _msDisplay, v => MsHundredths = v);
    partial void OnKmsTextChanged(string value) => ApplyClock(value, ref _kmsDigits, ref _kmsDisplay, v => KmsHundredths = v);
    partial void OnITextChanged(string value) => ApplyClock(value, ref _iDigits, ref _iDisplay, v => IHundredths = v);
    partial void OnIiTextChanged(string value) => ApplyClock(value, ref _iiDigits, ref _iiDisplay, v => IiHundredths = v);
    partial void OnIiiTextChanged(string value) => ApplyClock(value, ref _iiiDigits, ref _iiiDisplay, v => IiiHundredths = v);
    partial void OnIYunTextChanged(string value) => ApplyClock(value, ref _iYunDigits, ref _iYunDisplay, v => IYunHundredths = v);
    partial void OnIiYunTextChanged(string value) => ApplyClock(value, ref _iiYunDigits, ref _iiYunDisplay, v => IiYunHundredths = v);

    private static void ApplyClock(string value, ref string digits, ref string display, Action<int?> setHundredths)
    {
        var update = SwimTimeInput.FromClockText(value, digits, display);
        digits = update.Digits;
        display = update.ClockText;
        setHundredths(update.Hundredths);
    }

    partial void OnMsmkHundredthsChanged(int? value) => SyncText(ref _msmkText, nameof(MsmkText), ref _msmkDisplay, value);
    partial void OnMsHundredthsChanged(int? value) => SyncText(ref _msText, nameof(MsText), ref _msDisplay, value);
    partial void OnKmsHundredthsChanged(int? value) => SyncText(ref _kmsText, nameof(KmsText), ref _kmsDisplay, value);
    partial void OnIHundredthsChanged(int? value) => SyncText(ref _iText, nameof(IText), ref _iDisplay, value);
    partial void OnIiHundredthsChanged(int? value) => SyncText(ref _iiText, nameof(IiText), ref _iiDisplay, value);
    partial void OnIiiHundredthsChanged(int? value) => SyncText(ref _iiiText, nameof(IiiText), ref _iiiDisplay, value);
    partial void OnIYunHundredthsChanged(int? value) => SyncText(ref _iYunText, nameof(IYunText), ref _iYunDisplay, value);
    partial void OnIiYunHundredthsChanged(int? value) => SyncText(ref _iiYunText, nameof(IiYunText), ref _iiYunDisplay, value);

    private void SyncText(ref string field, string propertyName, ref string display, int? hundredths)
    {
        var formatted = SwimTimeInput.Format(hundredths);
        display = formatted;
        if (string.Equals(field, formatted, StringComparison.Ordinal))
            return;
        field = formatted;
        OnPropertyChanged(propertyName);
    }

    public int GetHundredths(Category category) => category switch
    {
        Category.IMoS => MsmkHundredths ?? 0,
        Category.MoS => MsHundredths ?? 0,
        Category.CMoS => KmsHundredths ?? 0,
        Category.FirstAdult => IHundredths ?? 0,
        Category.SecondAdult => IiHundredths ?? 0,
        Category.ThirdAdult => IiiHundredths ?? 0,
        Category.FirstJunior => IYunHundredths ?? 0,
        Category.SecondJunior => IiYunHundredths ?? 0,
        _ => 0
    };
}
