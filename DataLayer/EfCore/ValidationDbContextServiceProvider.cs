// Copyright (c) 2017 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT licence. See License.txt in the project root for license information.

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