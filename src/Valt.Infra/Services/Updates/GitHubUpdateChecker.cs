using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Valt.Infra.Services.Updates;

internal class GitHubUpdateChecker : IUpdateChecker
{
    private const string GitHubApiUrl = "https://api.github.com/repos/btcdoomguy/valt/releases?per_page=10";
    private readonly ILogger<GitHubUpdateChecker> _logger;

    public GitHubUpdateChecker(ILogger<GitHubUpdateChecker> logger)
    {
        _logger = logger;
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "Valt-Desktop-App");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        try
        {
            var response = await client.GetAsync(GitHubApiUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var releases = JsonSerializer.Deserialize<List<GitHubReleaseResponse>>(json, options);

            if (releases is null || releases.Count == 0)
            {
                _logger.LogWarning("Failed to deserialize GitHub releases response or no releases found");
                return null;
            }

            var release = releases.FirstOrDefault(r => !r.Draft && !r.Prerelease);
            if (release is null)
            {
                _logger.LogDebug("No stable release found");
                return null;
            }

            var tagVersion = ParseVersion(release.TagName);
            if (tagVersion is null || tagVersion <= currentVersion)
            {
                _logger.LogDebug("No update available. Current: {Current}, Latest: {Latest}",
                    currentVersion, release.TagName);
                return null;
            }

            var assets = release.Assets
                .Select(a => new ReleaseAsset(a.Name, a.BrowserDownloadUrl, a.Size))
                .ToList();

            return new UpdateInfo(
                release.TagName,
                release.Body ?? string.Empty,
                release.HtmlUrl,
                release.PublishedAt,
                assets);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to check for updates - network error");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Failed to check for updates - timeout");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while checking for updates");
            return null;
        }
    }

    private static Version? ParseVersion(string tagName)
    {
        var versionString = tagName.TrimStart('v', 'V');
        return Version.TryParse(versionString, out var version) ? version : null;
    }

    #region GitHub API Response DTOs

    private record GitHubReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("draft")]
        public bool Draft { get; init; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; init; }

        [JsonPropertyName("body")]
        public string? Body { get; init; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; } = string.Empty;

        [JsonPropertyName("published_at")]
        public DateTimeOffset PublishedAt { get; init; }

        [JsonPropertyName("assets")]
        public List<GitHubAssetResponse> Assets { get; init; } = new();
    }

    private record GitHubAssetResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; init; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; init; }
    }

    #endregion
}
