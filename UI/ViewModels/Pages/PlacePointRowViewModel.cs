using CommunityToolkit.Mvvm.ComponentModel;
using UI.Resources;

namespace UI.ViewModels.Pages;

public sealed partial class PlacePointRowViewModel : ObservableObject
{
    public PlacePointRowViewModel(int place, int points)
    {
        Place = place;
        _points = Math.Max(0, points);
    }

    public int Place { get; }

    public string PlaceTitle => string.Format(Strings.Settings_Scoring_PlaceRow, Place);

    [ObservableProperty] private int _points;

    public string PointsText
    {
        get => Points.ToString();
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Points = 0;
                return;
            }

            if (int.TryParse(value.Trim(), out var parsed) && parsed >= 0)
                Points = parsed;
            OnPropertyChanged();
        }
    }

    public void RefreshDisplayText() => OnPropertyChanged(nameof(PlaceTitle));

    partial void OnPointsChanged(int value)
    {
        if (value < 0)
            Points = 0;
        else
            OnPropertyChanged(nameof(PointsText));
    }
}
