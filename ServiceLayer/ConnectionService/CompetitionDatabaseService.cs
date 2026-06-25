using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.ConnectionService;

public interface ICompetitionDatabaseService
{
    Task<CompetitionOpenResult> TryOpenAsync(string filePath, CancellationToken cancellationToken = default);
}

public sealed record CompetitionOpenResult(bool Success, string? ErrorMessage);

public sealed class CompetitionDatabaseService(IDatabaseConnection databaseConnection) : ICompetitionDatabaseService
{
    public async Task<CompetitionOpenResult> TryOpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(filePath);
        var connectionString = $"Data Source={fullPath}";

        try
        {
            var options = new DbContextOptionsBuilder<EfCoreContext>().UseSwimDocSqlite().Options;
            var validationConnection = new DatabaseConnectionService();
            validationConnection.SetConnection(connectionString);

            await using var context = new EfCoreContext(options, validationConnection);
            if (!await context.Database.CanConnectAsync(cancellationToken))
                return new CompetitionOpenResult(false, null);

            databaseConnection.SetConnection(connectionString);
            return new CompetitionOpenResult(true, null);
        }
        catch (Exception ex)
        {
            return new CompetitionOpenResult(false, ex.Message);
        }
    }
}
