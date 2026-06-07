using DataLayer.Display;
using DataLayer.EfClasses;

namespace BizLogic.HeatAllocation;

public class HeatAllocationInDto
{
    public HeatAllocationInDto(HeatAllocationParameters parameters, SwimEvent swimEvent)
    {
        _parameters = parameters;
        if (SwimEventLaneNames.HasCustomLaneNames(swimEvent))
        {
            UsesCustomLaneNames = true;
            LaneMin = SwimEventLaneNames.GetLaneSlotMin(swimEvent);
            LaneMax = SwimEventLaneNames.GetLaneSlotMax(swimEvent);
            LaneCount = SwimEventLaneNames.GetLaneCount(swimEvent);
        }
        else
        {
            LaneMin = swimEvent.LaneMin;
            LaneMax = swimEvent.LaneMax;
            LaneCount = LaneMax - LaneMin + 1;
        }
    }

    private readonly HeatAllocationParameters _parameters;
    public int SwimEventId => _parameters.SwimEventId;
    public HeatOrder HeatOrder => _parameters.HeatOrder;
    public int MinHeatSize => _parameters.MinHeatSize;
    public bool UsesCustomLaneNames { get; }
    public int LaneMin { get; }
    public int LaneMax { get; }
    public int LaneCount { get; }
}
