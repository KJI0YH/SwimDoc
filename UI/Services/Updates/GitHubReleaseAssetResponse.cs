using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace UI.Services.Updates;

internal sealed class GitHubReleaseAssetResponse
{
    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; init; }
}
