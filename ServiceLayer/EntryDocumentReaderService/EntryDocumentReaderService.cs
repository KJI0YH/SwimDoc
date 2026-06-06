using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizDbAccess;
using BizLogic.EntryDocumentReaderLogic;
using BizLogic.EntryDocumentReaderLogic.Concrete;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.BizRunners;
using ServiceLayer.EntryDocumentReaderService.Exceptions;

namespace ServiceLayer.EntryDocumentReaderService;

public class EntryDocumentReaderService(EfCoreContext context) : IEntryDocumentReaderService
{
    private readonly EfCoreContext _context = context;
    private readonly RunnerWriteDbWithValidation<string, IReadOnlyList<EntryDocument>> _runner = new(
        new EntryDocumentReadAction(new EntryDocumentReaderDbAccess(context)), context);
    public IImmutableList<ValidationResult> Errors => _runner.Errors;

    public IReadOnlyList<EntryDocument> Read(string filePath)
    {
        var result = _runner.RunAction(filePath);
        return _runner.HasErrors ? throw new EntryDocumentReaderException() : result;
    }

    public (IReadOnlyList<EntryDocument> documents, EntryImportStats stats) ReadWithStats(
        string filePath,
        CancellationToken cancellationToken = default,
        bool saveChanges = true)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var action = new EntryDocumentReadAction(new EntryDocumentReaderDbAccess(_context));
        var documents = action.Action(filePath, cancellationToken);

        if (action.Errors.Any())
            throw new EntryDocumentReaderException();

        cancellationToken.ThrowIfCancellationRequested();

        _context.ChangeTracker.DetectChanges();

        ImportDocumentStats.CountScanned(
            documents,
            out var clubsScanned,
            out var clubsWithErrors,
            out var athletesScanned,
            out var athletesWithErrors,
            out var entriesScanned,
            out var entriesWithErrors);

        var clubsAdded = _context.ChangeTracker.Entries<Club>().Count(e => e.State == EntityState.Added);
        var clubsUpdated = _context.ChangeTracker.Entries<Club>().Count(e => e.State == EntityState.Modified);
        var athletesAdded = _context.ChangeTracker.Entries<Athlete>().Count(e => e.State == EntityState.Added);
        var athletesUpdated = _context.ChangeTracker.Entries<Athlete>().Count(e => e.State == EntityState.Modified);
        var entriesAdded = _context.ChangeTracker.Entries<Entry>().Count(e => e.State == EntityState.Added);
        var entriesUpdated = _context.ChangeTracker.Entries<Entry>().Count(e => e.State == EntityState.Modified);

        if (saveChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var errors = _context.SaveChangesWithValidation();
            if (errors.Any())
                throw new EntryDocumentReaderException();
        }

        return (documents, new EntryImportStats(
            clubsScanned,
            clubsWithErrors,
            clubsAdded,
            clubsUpdated,
            athletesScanned,
            athletesWithErrors,
            athletesAdded,
            athletesUpdated,
            entriesScanned,
            entriesWithErrors,
            entriesAdded,
            entriesUpdated));
    }
}
