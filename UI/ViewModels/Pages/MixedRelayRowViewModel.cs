using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer.EfClasses;
using static UI.Models.BaseTimes.BaseTimesSwimStyleCatalog;

namespace UI.ViewModels.Pages;

public sealed partial class MixedRelayRowViewModel : ObservableObject
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
    [ObservableProperty] private string _mixedBaseTimeText;
    [ObservableProperty] private string _mixedSecondsText;
    [ObservableProperty] private int? _mixedBaseTimeHundredths;
    public MixedRelayRowViewModel(
        Course course,
        int distance,
        Stroke stroke,
        int relayCount,
        int mixedHundredthsFromStore)
    {
        Course = course;
        Distance = distance;
        Stroke = stroke;
        RelayCount = relayCount;
        _name = FormatDisplayName();
        MixedBaseTimeHundredths = mixedHundredthsFromStore;
        _mixedBaseTimeText = SwimTimeInput.Format(MixedBaseTimeHundredths);
        _mixedSecondsText = SwimTimeInput.FormatSecondsField(MixedBaseTimeHundredths);
    }

    partial void OnMixedBaseTimeTextChanged(string value)
    {
        if (_suppressSync) return;
        var update = SwimTimeInput.FromClockText(value);
        MixedBaseTimeHundredths = update.Hundredths;
        _suppressSync = true;
        try { MixedSecondsText = update.SecondsText; }
        finally { _suppressSync = false; }
    }

    partial void OnMixedSecondsTextChanged(string value)
    {
        if (_suppressSync) return;
        var update = SwimTimeInput.FromSecondsText(value);
        MixedBaseTimeHundredths = update.Hundredths;
        _suppressSync = true;
        try
        {
            MixedSecondsText = update.SecondsText;
            MixedBaseTimeText = update.ClockText;
        }
        finally { _suppressSync = false; }
    }

    partial void OnMixedBaseTimeHundredthsChanged(int? value)
    {
        var formatted = SwimTimeInput.Format(value);
        if (!string.Equals(_mixedBaseTimeText, formatted, StringComparison.Ordinal))
        {
            _mixedBaseTimeText = formatted;
            OnPropertyChanged(nameof(MixedBaseTimeText));
        }
    }
}
