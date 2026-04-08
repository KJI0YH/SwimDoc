using DataLayer.EfClasses;

namespace BizLogic.HeatLogic;

public class HeatAllocationOutDto(List<Heat> heats, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
{
    public List<Heat> Heats { get; } = heats;
    public IReadOnlyList<string> Warnings = warnings;
    public IReadOnlyList<string> Errors = errors;
}