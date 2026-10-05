using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer.Display;
using DataLayer.EfClasses;
using UI.Resources;

namespace UI.ViewModels.Pages;

public sealed class ScoringCategoryOption(Category? category) : ObservableObject
{
    public Category? Category { get; } = category;

    public string DisplayText => Category is null
        ? Strings.Settings_Scoring_MinimumCategory_All
        : CategoryDisplay.Format(Category);

    public void RefreshDisplayText() => OnPropertyChanged(nameof(DisplayText));
}
