namespace UI.Services.Updates;

public interface IGitHubUpdateCheckService
{
    Task<GitHubUpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
