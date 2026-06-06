namespace UI.Services;

public interface IGitHubUpdateCheckService
{
    Task<GitHubUpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
