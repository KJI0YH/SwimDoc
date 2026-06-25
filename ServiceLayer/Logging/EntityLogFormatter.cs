using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryDocumentReaderService;

namespace ServiceLayer.Logging;

public static class EntityLogFormatter
{
    public static string FormatOperation(string operation, object? entity) =>
        DataLayer.Logging.EntityLogFormatter.FormatOperation(operation, entity);

    public static string FormatListRead(Type entityType, int totalCount, int loadedCount, string? criteria = null)
    {
        var countPart = loadedCount == totalCount
            ? $"{totalCount} items"
            : $"{loadedCount} of {totalCount} items";
        var criteriaPart = string.IsNullOrWhiteSpace(criteria) ? string.Empty : $" {criteria.Trim()}";
        return $"Read List {entityType.Name}: {countPart}{criteriaPart}";
    }

    public static string FormatImportStats(string filePath, EntryImportStats stats)
    {
        var fileName = Path.GetFileName(filePath);
        return
            $"Import entries from \"{fileName}\": clubs +{stats.ClubsAdded}/~{stats.ClubsUpdated}, athletes +{stats.AthletesAdded}/~{stats.AthletesUpdated}, entries +{stats.EntriesAdded}/~{stats.EntriesUpdated}";
    }

    public static string FormatIdList(IEnumerable<int> ids) =>
        DataLayer.Logging.EntityLogFormatter.FormatIdList(ids);

    public static void LogChangeTrackerChanges(IAppLog log, DbContext context)
    {
        var typeOrder = new Dictionary<string, int>
        {
            [nameof(DataLayer.EfClasses.Club)] = 0,
            [nameof(DataLayer.EfClasses.Athlete)] = 1,
            [nameof(DataLayer.EfClasses.Relay)] = 2,
            [nameof(DataLayer.EfClasses.Entry)] = 3,
        };

        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(tracked => tracked.State is EntityState.Added or EntityState.Modified)
                     .OrderBy(tracked => typeOrder.GetValueOrDefault(tracked.Metadata.ClrType.Name, 99))
                     .ThenBy(tracked => tracked.State))
            log.Info(FormatOperation(ToOperationName(entry.State), entry.Entity));
    }

    private static string ToOperationName(EntityState state) =>
        state switch
        {
            EntityState.Added => "Create",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => "Change"
        };
}
