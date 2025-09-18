using DataLayer.EfClasses;

namespace BizLogic.HeatLogic;

public class HeatAllocationOutDto
{
    public List<Heat> Heats { get; }

    public HeatAllocationOutDto(List<Heat> heats)
    {
        Heats = heats;
    }
}