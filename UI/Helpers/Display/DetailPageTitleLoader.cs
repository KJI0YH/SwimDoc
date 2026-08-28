using Microsoft.EntityFrameworkCore;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.SwimStyleService;

namespace UI.Helpers.Display;

public static class DetailPageTitleLoader
{
    public static async Task<string> LoadAthleteAsync(IAthleteService service, int id)
    {
        var athlete = await service.FindAsync(id).ConfigureAwait(false);
        return EntityDisplayFormatter.FormatAthleteName(athlete);
    }

    public static async Task<string> LoadClubAsync(IClubService service, int id)
    {
        var club = await service.FindAsync(id).ConfigureAwait(false);
        return club?.Name ?? string.Empty;
    }

    public static async Task<string> LoadEventAsync(IEventService service, int id)
    {
        var swimEvent = await service.Query()
            .Include(e => e.SwimStyle)
            .Include(e => e.AgeGroup)
            .FirstOrDefaultAsync(e => e.Id == id)
            .ConfigureAwait(false);
        return EntityDisplayFormatter.FormatSwimEvent(swimEvent);
    }

    public static async Task<string> LoadAgeGroupAsync(IAgeGroupService service, int id)
    {
        var ageGroup = await service.FindAsync(id).ConfigureAwait(false);
        return EntityDisplayFormatter.FormatAgeGroup(ageGroup);
    }

    public static async Task<string> LoadSwimStyleAsync(ISwimStyleService service, int id)
    {
        var style = await service.FindAsync(id).ConfigureAwait(false);
        return EntityDisplayFormatter.FormatSwimStyle(style);
    }

    public static async Task<string> LoadEntryAsync(IEntryService service, int id)
    {
        var entry = await service.Query()
            .Include(e => e.Athlete)
            .Include(e => e.Relay)
            .ThenInclude(relay => relay!.Club)
            .Include(e => e.Relay)
            .ThenInclude(relay => relay!.Positions)
            .ThenInclude(position => position.Athlete)
            .Include(e => e.SwimEvent)
            .ThenInclude(swimEvent => swimEvent!.SwimStyle)
            .Include(e => e.SwimEvent)
            .ThenInclude(swimEvent => swimEvent!.AgeGroup)
            .Include(e => e.SwimStyle)
            .FirstOrDefaultAsync(e => e.Id == id)
            .ConfigureAwait(false);

        var participant = EntityDisplayFormatter.FormatEntryParticipantName(entry);
        var swimName = EntityDisplayFormatter.FormatEntrySwimName(entry);
        if (string.IsNullOrWhiteSpace(participant))
            return swimName;
        return string.IsNullOrWhiteSpace(swimName) ? participant : $"{participant}, {swimName}";
    }
}
