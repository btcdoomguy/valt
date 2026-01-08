namespace Valt.Infra.Services.Updates;

public record UpdateInfo(
    string Version,
    string ReleaseNotes,
    string HtmlUrl,
    DateTimeOffset PublishedAt,
    IReadOnlyList<ReleaseAsset> Assets);

public record ReleaseAsset(
    string Name,
    string DownloadUrl,
    long Size);
