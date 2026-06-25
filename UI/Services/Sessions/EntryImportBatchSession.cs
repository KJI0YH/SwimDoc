using BizLogic.EntryDocumentReader;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryDocumentReaderService.Exceptions;
using ServiceLayer.EntryImportSettings;
using ServiceLayer.Logging;

namespace UI.Services.Sessions;

/// <summary>
/// Batch import must use a single <see cref="EfCoreContext"/> for parsing and commit.
/// Do not resolve <see cref="IEntryDocumentReaderService"/> from DI here — it would get a different context.
/// </summary>
public sealed class EntryImportBatchSession : IAsyncDisposable
{
    private readonly IServiceScope _scope = App.Current.Services.CreateScope();
    private readonly EfCoreContext _dbContext;
    private readonly IEntryDocumentReaderService _reader;
    private readonly IAppLog _log;

    public EntryImportBatchSession()
    {
        _dbContext = _scope.ServiceProvider.GetRequiredService<EfCoreContext>();
        _log = _scope.ServiceProvider.GetRequiredService<IAppLog>();
        _reader = new EntryDocumentReaderService(
            _dbContext,
            _scope.ServiceProvider.GetRequiredService<IEntryImportSettingsService>(),
            _log);
    }

    public (IReadOnlyList<EntryDocument> documents, EntryImportStats stats) ImportFile(
        string filePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = _reader.ReadWithStats(filePath, cancellationToken, saveChanges: false);
        cancellationToken.ThrowIfCancellationRequested();
        using var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            var errors = _dbContext.SaveChangesWithValidation();
            if (errors.Count > 0)
                throw new EntryDocumentReaderException();
            transaction.Commit();
            _log.Info($"Entry import committed to database: \"{filePath}\"");
            return result;
        }
        catch
        {
            transaction.Rollback();
            _dbContext.ChangeTracker.Clear();
            _log.Warning($"Entry import rolled back: \"{filePath}\"");
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        _scope.Dispose();
        return ValueTask.CompletedTask;
    }
}
