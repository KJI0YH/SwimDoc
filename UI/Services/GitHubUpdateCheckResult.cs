namespace UI.Services;

public sealed record GitHubUpdateCheckResult(
    GitHubUpdateCheckStatus Status,
    string? LatestVersion = null,
    string? DownloadUrl = null,
    string? ErrorMessage = null);
