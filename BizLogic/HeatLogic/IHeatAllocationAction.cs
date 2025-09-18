using BizLogic.GenericInterfaces;
using DataLayer.EfClasses;

namespace BizLogic.HeatLogic;

public interface IHeatAllocationAction : IBizAction<HeatAllocationInDto, HeatAllocationOutDto>
{
}