using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizDbAccess.EntryDocumentReader;
using BizLogic.EntryDocumentReader;
using BizLogic.EntryDocumentReader.Concrete;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryDocumentReaderService.Exceptions;
using ServiceLayer.EntryImportSettings;

namespace ServiceLayer.EntryDocumentReaderService;

public class EntryDocumentReaderService(
    EfCoreContext context,
    IEntryImportSettingsService importSettings) : IEntryDocumentReaderService
{
    private readonly EfCoreContext _context = context;
    private readonly IEntryImportSettingsService _importSettings = importSettings;

    public IImmutableList<ValidationResult> Errors { get; private set; } = ImmutableList<ValidationResult>.Empty;

    public IReadOnlyList<EntryDocument> Read(string filePath)
    {
        var action = CreateReadAction();
        var result = action.Action(filePath);
        Errors = action.Errors.ToImmutableList();
        if (action.Errors.Any())
            throw new EntryDocumentReaderException();
        return result;
    }

    public (IReadOnlyList<EntryDocument> documents, EntryImportStats stats) ReadWithStats(
        string filePath,
        CancellationToken cancellationToken = default,
        bool saveChanges = true)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var action = CreateReadAction();
        var documents = action.Action(filePath, cancellationToken);
        Errors = action.Errors.ToImmutableList();
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

    private EntryDocumentReadAction CreateReadAction() =>
        new(new EntryDocumentReaderDbAccess(_context), _importSettings.HighlightScoringMode);
}