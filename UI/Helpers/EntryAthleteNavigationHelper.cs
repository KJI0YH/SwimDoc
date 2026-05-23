using DataLayer.EfClasses;

namespace UI.Helpers;

public static class EntryAthleteNavigationHelper
{
    public static bool TryGetAthleteId(Entry? entry, out int athleteId)
    {
        if (entry?.AthleteId is int id)
        {
            athleteId = id;
            return true;
        }

        athleteId = 0;
        return false;
    }
}
