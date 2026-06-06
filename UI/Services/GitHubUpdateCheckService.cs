using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace UI.Services;

public sealed class GitHubUpdateCheckService(HttpClient httpClient) : IGitHubUpdateCheckService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/KJI0YH/SwimDoc/releases/latest";
    private const string ReleasesUrl = "https://api.github.com/repos/KJI0YH/SwimDoc/releases?per_page=1";

    public async Task<GitHubUpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            GitHubReleaseResponse? release;
            using (var latestResponse = await httpClient.GetAsync(LatestReleaseUrl, cancellationToken))
            {
                if (latestResponse.IsSuccessStatusCode)
                {
                    release = await latestResponse.Content.ReadFromJsonAsync<GitHubReleaseResponse>(cancellationToken);
                }
                else if (latestResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    release = await TryGetLatestFromReleasesListAsync(cancellationToken);
                }
                else
                {
                    latestResponse.EnsureSuccessStatusCode();
                    release = null;
                }
            }

            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
                return new GitHubUpdateCheckResult(GitHubUpdateCheckStatus.NoReleases);

            if (!TryParseVersion(release.TagName, out var latestVersion))
                return new GitHubUpdateCheckResult(GitHubUpdateCheckStatus.Failed);

            if (!TryParseVersion(AppVersionInformation.Display, out var currentVersion))
                currentVersion = new Version(0, 0, 0);

            if (latestVersion <= currentVersion)
                return new GitHubUpdateCheckResult(GitHubUpdateCheckStatus.UpToDate, release.TagName);

            var downloadUrl = ResolveDownloadUrl(release);
            return new GitHubUpdateCheckResult(
                GitHubUpdateCheckStatus.UpdateAvailable,
                release.TagName,
                downloadUrl);
        }
        catch (HttpRequestException ex)
        {
            return new GitHubUpdateCheckResult(GitHubUpdateCheckStatus.Failed, ErrorMessage: ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new GitHubUpdateCheckResult(GitHubUpdateCheckStatus.Failed, ErrorMessage: ex.Message);
        }
        catch (Exception ex)
        {
            return new GitHubUpdateCheckResult(GitHubUpdateCheckStatus.Failed, ErrorMessage: ex.Message);
        }
    }

    private async Task<GitHubReleaseResponse?> TryGetLatestFromReleasesListAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(ReleasesUrl, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var releases = await response.Content.ReadFromJsonAsync<GitHubReleaseResponse[]>(cancellationToken);
        return releases is { Length: > 0 } ? releases[0] : null;
    }

    private static string? ResolveDownloadUrl(GitHubReleaseResponse release)
    {
        var assetUrl = release.Assets?
            .Select(asset => asset.BrowserDownloadUrl)
            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url) &&
                                   (url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                                    url.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                                    url.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)));

        return assetUrl ?? release.HtmlUrl;
    }

    public static bool TryParseVersion(string tag, out Version version)
    {
        var normalized = tag.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[1..];

        var plusIndex = normalized.IndexOf('+');
        if (plusIndex >= 0)
            normalized = normalized[..plusIndex];

        return Version.TryParse(normalized, out version!);
    }
}
