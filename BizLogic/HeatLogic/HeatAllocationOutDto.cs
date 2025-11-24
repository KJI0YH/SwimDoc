using DataLayer.EfClasses;

namespace BizLogic.HeatLogic;

public class HeatAllocationOutDto(List<Heat> heats)
{
    public List<Heat> Heats { get; } = heats;
}