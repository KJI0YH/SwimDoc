using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.SwimStyleService;

public interface ISwimStyleService : ICrudService<SwimStyle, int?>
{
}
