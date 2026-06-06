namespace UI.Models.Operations;

public readonly record struct OperationItemOutcome(
    OperationItemStatus Status,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    int? HeatsCreatedCount = null)
{
    public static OperationItemOutcome Success(int? heatsCreatedCount = null) =>
        new(OperationItemStatus.Completed, [], [], heatsCreatedCount);

    public static OperationItemOutcome WithWarnings(
        IReadOnlyList<string> warnings,
        int? heatsCreatedCount = null) =>
        new(OperationItemStatus.CompletedWithWarnings, warnings, [], heatsCreatedCount);

    public static OperationItemOutcome Failed(
        IReadOnlyList<string> errors,
        IReadOnlyList<string>? warnings = null,
        int? heatsCreatedCount = null) =>
        new(OperationItemStatus.Failed, warnings ?? [], errors, heatsCreatedCount);

    public static OperationItemOutcome Skipped() =>
        new(OperationItemStatus.Skipped, [], [], null);
}
