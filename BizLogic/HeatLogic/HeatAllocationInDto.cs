using DataLayer.EfClasses;

namespace BizLogic.HeatLogic;

public class HeatAllocationInDto
{
    public HeatAllocationInDto(HeatAllocationParameters parameters, int laneMin, int laneMax)
    {
        _parameters = parameters;
        LaneMin = laneMin;
        LaneMax = laneMax;
        LaneCount = LaneMax - LaneMin + 1;
    }

    private readonly HeatAllocationParameters _parameters;
    public int SwimEventId => _parameters.SwimEventId;
    public HeatOrder HeatOrder => _parameters.HeatOrder;
    public int MinHeatSize => _parameters.MinHeatSize;
    public int LaneMin { get; }
    public int LaneMax { get; }
    public int LaneCount { get; }

}