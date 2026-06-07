using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer.EfClasses;
using static UI.Models.BaseTimes.BaseTimesSwimStyleCatalog;

namespace UI.ViewModels.Pages;

public sealed partial class BaseTimeTableRowViewModel : ObservableObject
{
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
        Course course,
        int distance,
        Stroke stroke,
        int relayCount,
        int menHundredthsFromStore,
        int womenHundredthsFromStore)
    {
        Course = course;
        Distance = distance;
        Stroke = stroke;
        RelayCount = relayCount;
        _name = FormatDisplayName();
        MenBaseTimeHundredths = menHundredthsFromStore;
        WomenBaseTimeHundredths = womenHundredthsFromStore;
        _menBaseTimeText = SwimTimeInput.Format(MenBaseTimeHundredths);
        _womenBaseTimeText = SwimTimeInput.Format(WomenBaseTimeHundredths);
        _menSecondsText = SwimTimeInput.FormatSecondsField(MenBaseTimeHundredths);
        _womenSecondsText = SwimTimeInput.FormatSecondsField(WomenBaseTimeHundredths);
    }

    partial void OnMenBaseTimeTextChanged(string value)
    {
        if (_suppressSync) return;
        var update = SwimTimeInput.FromClockText(value);
        MenBaseTimeHundredths = update.Hundredths;
        _suppressSync = true;
        try { MenSecondsText = update.SecondsText; }
        finally { _suppressSync = false; }
    }

    partial void OnWomenBaseTimeTextChanged(string value)
    {
        if (_suppressSync) return;
        var update = SwimTimeInput.FromClockText(value);
        WomenBaseTimeHundredths = update.Hundredths;
        _suppressSync = true;
        try { WomenSecondsText = update.SecondsText; }
        finally { _suppressSync = false; }
    }

    partial void OnMenSecondsTextChanged(string value)
    {
        if (_suppressSync) return;
        var update = SwimTimeInput.FromSecondsText(value);
        MenBaseTimeHundredths = update.Hundredths;
        _suppressSync = true;
        try
        {
            MenSecondsText = update.SecondsText;
            MenBaseTimeText = update.ClockText;
        }
        finally { _suppressSync = false; }
    }

    partial void OnWomenSecondsTextChanged(string value)
    {
        if (_suppressSync) return;
        var update = SwimTimeInput.FromSecondsText(value);
        WomenBaseTimeHundredths = update.Hundredths;
        _suppressSync = true;
        try
        {
            WomenSecondsText = update.SecondsText;
            WomenBaseTimeText = update.ClockText;
        }
        finally { _suppressSync = false; }
    }

    partial void OnMenBaseTimeHundredthsChanged(int? value)
    {
        var formatted = SwimTimeInput.Format(value);
        if (!string.Equals(_menBaseTimeText, formatted, StringComparison.Ordinal))
        {
            _menBaseTimeText = formatted;
            OnPropertyChanged(nameof(MenBaseTimeText));
        }
    }

    partial void OnWomenBaseTimeHundredthsChanged(int? value)
    {
        var formatted = SwimTimeInput.Format(value);
        if (!string.Equals(_womenBaseTimeText, formatted, StringComparison.Ordinal))
        {
            _womenBaseTimeText = formatted;
            OnPropertyChanged(nameof(WomenBaseTimeText));
        }
    }
}
