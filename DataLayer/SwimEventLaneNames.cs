using DataLayer.EfClasses;

namespace DataLayer;

public static class SwimEventLaneNames
{
    public static IReadOnlyList<string> Parse(string? customLaneNames)
    {
        if (string.IsNullOrWhiteSpace(customLaneNames))
            return [];

        return customLaneNames
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    public static bool HasCustomLaneNames(SwimEvent swimEvent) =>
        Parse(swimEvent.CustomLaneNames).Count > 0;

    public static int GetLaneCount(SwimEvent swimEvent) =>
        HasCustomLaneNames(swimEvent)
            ? Parse(swimEvent.CustomLaneNames).Count
            : swimEvent.LaneMax - swimEvent.LaneMin + 1;

    public static int GetLaneSlotMin(SwimEvent swimEvent) =>
        HasCustomLaneNames(swimEvent) ? 1 : swimEvent.LaneMin;

    public static int GetLaneSlotMax(SwimEvent swimEvent) =>
        HasCustomLaneNames(swimEvent)
            ? Parse(swimEvent.CustomLaneNames).Count
            : swimEvent.LaneMax;

    public static bool IsLaneInRange(SwimEvent swimEvent, int lane) =>
        lane >= GetLaneSlotMin(swimEvent) && lane <= GetLaneSlotMax(swimEvent);

    public static string GetLaneDisplay(SwimEvent swimEvent, int lane)
    {
        if (HasCustomLaneNames(swimEvent))
        {
            var names = Parse(swimEvent.CustomLaneNames);
            var index = lane - 1;
            if (index >= 0 && index < names.Count)
                return names[index];
        }

        return lane.ToString();
    }

    public static string FormatLanesSummary(SwimEvent swimEvent)
    {
        if (HasCustomLaneNames(swimEvent))
        {
            var names = Parse(swimEvent.CustomLaneNames);
            return names.Count switch
            {
                0 => string.Empty,
                1 => names[0],
                _ => $"{names[0]}-{names[^1]}"
            };
        }

        return $"{swimEvent.LaneMin}-{swimEvent.LaneMax}";
    }

    public static IEnumerable<LaneSlot> GetLaneSlots(SwimEvent swimEvent)
    {
        if (HasCustomLaneNames(swimEvent))
        {
            var names = Parse(swimEvent.CustomLaneNames);
            for (var i = 0; i < names.Count; i++)
                yield return new LaneSlot(i + 1, names[i]);
        }
        else
        {
            for (var lane = swimEvent.LaneMin; lane <= swimEvent.LaneMax; lane++)
                yield return new LaneSlot(lane, lane.ToString());
        }
    }
}
