namespace BizLogic.HeatLogic;

public class HeatAllocationParameters(int swimEventId, HeatOrder heatOrder, int minHeatSize)
{
    public int SwimEventId { get; } = swimEventId;
    public HeatOrder HeatOrder { get; } = heatOrder;
    public int MinHeatSize { get; } = minHeatSize;
}