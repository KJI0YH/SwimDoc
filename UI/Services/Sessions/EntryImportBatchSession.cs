using BizLogic.EntryDocumentReader;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryDocumentReaderService.Exceptions;
using ServiceLayer.EntryImportSettings;

namespace UI.Services.Sessions;

public sealed class EntryImportBatchSession : IAsyncDisposable
{
    private readonly IServiceScope _scope = App.Current.Services.CreateScope();
    private readonly EfCoreContext _dbContext;
    private readonly EntryDocumentReaderService _reader;

    public EntryImportBatchSession()
    {
        _dbContext = _scope.ServiceProvider.GetRequiredService<EfCoreContext>();
        var importSettings = _scope.ServiceProvider.GetRequiredService<IEntryImportSettingsService>();
        _reader = new EntryDocumentReaderService(_dbContext, importSettings);
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
