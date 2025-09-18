using BizDbAccess;
using BizLogic.GenericInterfaces;
using BizLogic.Helpers;
using DataLayer.EfClasses;

namespace BizLogic.HeatLogic;

public class HeatAllocationAction :
    BizActionErrors,
    IBizAction<HeatAllocationInDto, HeatAllocationOutDto>
{
    private readonly IHeatAllocationDbAccess _dbAccess;

    public HeatAllocationAction(IHeatAllocationDbAccess dbAccess)
    {
        _dbAccess = dbAccess;
    }

    public HeatAllocationOutDto Action(HeatAllocationInDto dataIn)
    {
        var isHeatsAlreadyExists = _dbAccess.IsHeatsExists(dataIn.SwimEventId);
        if (isHeatsAlreadyExists)
        {
            AddError($"Heats for event with id {dataIn.SwimEventId} already exists");
            return null;
        }

        var entries = new BufferedCollection<Entry>(_dbAccess.GetOrderedEntriesByEventId(dataIn.SwimEventId));
        if (entries.Count == 0)
        {
            AddError($"There is no entries for event with id {dataIn.SwimEventId}");
            return null;
        }

        var heatNumbers = OrderHeatNumbers(entries.Count, dataIn.LaneCount, dataIn.HeatOrder);
        var laneNumbers = OrderLaneNumbers(dataIn.LaneMin, dataIn.LaneMax);
        var heats = new List<Heat>();

        if (!IsWeakHeatFull(entries.Count, dataIn.LaneCount))
        {
            var heatSize = Math.Max(dataIn.MinHeatSize, entries.Count % dataIn.LaneCount);
            var weakHeat = CreateHeat(entries.TakeLast(heatSize), laneNumbers, heatNumbers.TakeLast(), dataIn.SwimEventId);
            heats.Add(weakHeat);
        }

        while (!entries.IsEmpty)
        {
            var heat = CreateHeat(entries.TakeFirst(dataIn.LaneCount), laneNumbers, heatNumbers.TakeFirst(), dataIn.SwimEventId);
            heats.Add(heat);
        }

        _dbAccess.AddHeats(heats);
        return new HeatAllocationOutDto(heats);
    }

    private Heat CreateHeat(IEnumerable<Entry> entries, int[] laneNumbers, int heatNumber, int swimEventId)
    {
        var heat = new Heat
        {
            Number = heatNumber,
            SwimEventId = swimEventId,
            Status = HeatStatus.SEEDED
        };
        var laneIndex = 0;
        foreach (var entry in entries)
        {
            heat.Positions.Add(new HeatPosition
            {
                EntryId = entry.Id,
                Lane = laneNumbers[laneIndex++],
            });
        }

        return heat;
    }

    private BufferedCollection<int> OrderHeatNumbers(int entryCount, int laneCount, HeatOrder order)
    {
        var heatCount = entryCount / laneCount;
        if (entryCount % laneCount != 0)
            heatCount++;
        var heatNumbers = Enumerable.Range(1, heatCount);
        if (order == HeatOrder.FromWeakToStrong)
            heatNumbers = heatNumbers.Reverse();
        return new BufferedCollection<int>(heatNumbers);
    }

    private int[] OrderLaneNumbers(int laneMin, int laneMax)
    {
        List<int> laneNumbers = [];
        var laneCount = laneMax - laneMin + 1;
        var laneCenter = laneCount / 2 + laneCount % 2;
        laneNumbers.Add(laneCenter);
        var sign = 1;
        var shift = 0;
        for (var i = 1; i < laneCount; i++)
        {
            shift += i * sign;
            sign *= -1;
            laneNumbers.Add(laneCenter + shift);
        }

        return laneNumbers.ToArray();
    }

    private bool IsWeakHeatFull(int entryCount, int laneCount)
    {
        return entryCount % laneCount == 0;
    }
}