using UI.ViewModels.Pages;
using UI.Views.Pages;

namespace UI.Helpers.Navigation;

public static class NavigationPageRegistry
{
    public static readonly HashSet<Type> CompetitionViewModelTypes =
    [
        typeof(EventsViewModel),
        typeof(EntriesViewModel),
        typeof(HeatsViewModel),
        typeof(FixationViewModel),
        typeof(ResultsViewModel),
        typeof(AthletesViewModel),
        typeof(ClubsViewModel),
        typeof(AgeGroupsViewModel),
        typeof(SwimStylesViewModel)
    ];

    public static readonly HashSet<Type> RootViewModelTypes =
    [
        ..CompetitionViewModelTypes,
        typeof(SettingsViewModel),
        typeof(AboutViewModel)
    ];

    public static readonly Dictionary<string, Type> SidebarTagToViewModelType = new()
    {
        ["Events"] = typeof(EventsViewModel),
        ["Entries"] = typeof(EntriesViewModel),
        ["Heats"] = typeof(HeatsViewModel),
        ["HeatsResults"] = typeof(FixationViewModel),
        ["Results"] = typeof(ResultsViewModel),
        ["Athletes"] = typeof(AthletesViewModel),
        ["Clubs"] = typeof(ClubsViewModel),
        ["AgeGroups"] = typeof(AgeGroupsViewModel),
        ["SwimStyles"] = typeof(SwimStylesViewModel),
        ["Settings"] = typeof(SettingsViewModel),
        ["About"] = typeof(AboutViewModel)
    };

    public static readonly Dictionary<Type, string> ViewModelTypeToSidebarTag =
        SidebarTagToViewModelType.ToDictionary(pair => pair.Value, pair => pair.Key);

    public static readonly HashSet<Type> DetailViewModelTypes =
    [
        typeof(AthleteDetailsViewModel),
        typeof(ClubDetailsViewModel),
        typeof(EntryDetailsViewModel),
        typeof(EventDetailsViewModel),
        typeof(AgeGroupDetailsViewModel),
        typeof(SwimStyleDetailsViewModel)
    ];

    public static readonly Dictionary<Type, Type> ViewModelTypeToPageType = new()
    {
        [typeof(CompetitionSelectionViewModel)] = typeof(CompetitionSelectionPage),
        [typeof(EventsViewModel)] = typeof(EventsPage),
        [typeof(HeatsViewModel)] = typeof(HeatsPage),
        [typeof(FixationViewModel)] = typeof(FixationPage),
        [typeof(ResultsViewModel)] = typeof(ResultsPage),
        [typeof(EntriesViewModel)] = typeof(EntriesPage),
        [typeof(AthletesViewModel)] = typeof(AthletesPage),
        [typeof(ClubsViewModel)] = typeof(ClubsPage),
        [typeof(AgeGroupsViewModel)] = typeof(AgeGroupsPage),
        [typeof(SwimStylesViewModel)] = typeof(SwimStylesPage),
        [typeof(AthleteDetailsViewModel)] = typeof(AthleteDetailsPage),
        [typeof(ClubDetailsViewModel)] = typeof(ClubDetailsPage),
        [typeof(EntryDetailsViewModel)] = typeof(EntryDetailsPage),
        [typeof(EventDetailsViewModel)] = typeof(EventDetailsPage),
        [typeof(AgeGroupDetailsViewModel)] = typeof(AgeGroupDetailsPage),
        [typeof(SwimStyleDetailsViewModel)] = typeof(SwimStyleDetailsPage),
        [typeof(SettingsViewModel)] = typeof(SettingsPage),
        [typeof(AboutViewModel)] = typeof(AboutPage)
    };

    public static readonly Dictionary<Type, string> PageTypeToSidebarTag =
        ViewModelTypeToPageType
            .Where(pair => RootViewModelTypes.Contains(pair.Key))
            .ToDictionary(pair => pair.Value, pair => ViewModelTypeToSidebarTag[pair.Key]);

    public static bool IsDetailPage(Type pageType) =>
        ViewModelTypeToPageType.Any(pair => pair.Value == pageType && DetailViewModelTypes.Contains(pair.Key));

    public static bool IsSidebarPage(Type pageType) => PageTypeToSidebarTag.ContainsKey(pageType);

    public static bool RequiresCompetition(Type pageType) =>
        PageTypeToSidebarTag.TryGetValue(pageType, out var tag) && RequiresCompetitionForTag(tag);

    public static bool RequiresCompetitionForTag(string tag) =>
        SidebarTagToViewModelType.TryGetValue(tag, out var viewModelType)
        && CompetitionViewModelTypes.Contains(viewModelType);
}
