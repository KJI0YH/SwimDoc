using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizDbAccess;
using BizLogic.EntryDocumentReaderLogic;
using BizLogic.EntryDocumentReaderLogic.Concrete;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.BizRunners;
using ServiceLayer.EntryDocumentReaderService.Exceptions;

namespace ServiceLayer.EntryDocumentReaderService;

public class EntryDocumentReaderService : IEntryDocumentReaderService
{
    private readonly EfCoreContext _context;
    private readonly RunnerWriteDbWithValidation<string, IReadOnlyList<EntryDocument>> _runner;
    public IImmutableList<ValidationResult> Errors => _runner.Errors;

    public EntryDocumentReaderService(EfCoreContext context)
    {
        _context = context;
        _runner = new RunnerWriteDbWithValidation<string, IReadOnlyList<EntryDocument>>(
            new EntryDocumentReadAction(new EntryDocumentReaderDbAccess(context)), context);
    }

    public IReadOnlyList<EntryDocument> Read(string filePath)
    {
        var result = _runner.RunAction(filePath);
        return _runner.HasErrors ? throw new EntryDocumentReaderException() : result;
    }
}