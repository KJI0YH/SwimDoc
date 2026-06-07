using System.ComponentModel.DataAnnotations;
using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.EntryService;

public interface IEntryService : ICrudService<Entry, int?>
{
    Task<List<Entry>> GetEntriesByEventIdOrderByFinishTimeAsync(int eventId);
    Task<CombinedResultsData> GetCombinedResultsByAgeGroupAsync(int ageGroupId);
    Task<(List<Entry> Created, IReadOnlyList<ValidationResult> Errors)> CopyEntriesFromPreviousEventAsync(
        int previousEventId,
        int targetEventId,
        CancellationToken cancellationToken = default);
}
