namespace UI.Services;

public enum GitHubUpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    NoReleases,
    Failed
}

public sealed record GitHubUpdateCheckResult(
    GitHubUpdateCheckStatus Status,
    string? LatestVersion = null,
    string? DownloadUrl = null,
    string? ErrorMessage = null);
