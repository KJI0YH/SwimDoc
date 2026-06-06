namespace UI.Services;

public interface IConfirmDialogService
{
    Task<bool> ConfirmDeleteIfOfficialResultsAffectedAsync<TEntity>(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken = default);

    Task<bool> ConfirmHeatReformIfOfficialResultsExistAsync(
        int swimEventId,
        string eventDisplayName,
        CancellationToken cancellationToken = default);
}
