using DataLayer.EfCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EntryService;
using ServiceLayer.Logging;
using ServiceLayer.ReportGeneratorService;

namespace ServiceLayer.DependencyInjection;

public static class SharedDbContextServices
{
    /// <summary>
    /// Creates services that must share one <see cref="EfCoreContext"/> instance within an operation.
    /// Required because the context is registered as Transient.
    /// </summary>
    public static IReportExportService CreateReportExportService(IServiceProvider provider)
    {
        var dbContext = provider.GetRequiredService<EfCoreContext>();
        var log = provider.GetRequiredService<IAppLog>();
        return new ReportExportService(dbContext, new EntryService.EntryService(dbContext, log), log);
    }
}
