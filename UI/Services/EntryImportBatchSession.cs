using BizLogic.EntryDocumentReaderLogic;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryDocumentReaderService.Exceptions;

namespace UI.Services;

/// <summary>
/// Imports entry files using a single DbContext. Each file is read without holding a transaction,
/// then committed in a short per-file transaction so navigation and other pages can query the DB.
/// </summary>
public sealed class EntryImportBatchSession : IAsyncDisposable
{
    private readonly IServiceScope _scope = App.Current.Services.CreateScope();
    private readonly EfCoreContext _dbContext;
    private readonly EntryDocumentReaderService _reader;

    public EntryImportBatchSession()
    {
        _dbContext = _scope.ServiceProvider.GetRequiredService<EfCoreContext>();
        // DbContext is Transient: reader must use the same instance we commit.
        _reader = new EntryDocumentReaderService(_dbContext);
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
            return result;
        }
        catch
        {
            transaction.Rollback();
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        _scope.Dispose();
        return ValueTask.CompletedTask;
    }
}
