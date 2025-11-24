using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfCore
{
    public class ValidationDbContextServiceProvider(DbContext currContext) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(DbContext) ? currContext : null;
        }
    }
}