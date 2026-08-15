using System.Reflection;
using System.Text.Json;

namespace PasswordManager.Services;

/// <summary>
/// معلومات آخر إصدار متاح على GitHub.
/// </summary>
public sealed class UpdateInfo
{
    public string Tag { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string ReleaseUrl { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
}

/// <summary>
/// جلب آخر release من GitHub والتحقق من وجود تحديث جديد.
/// </summary>
public static class UpdateService
{
    private const string Repo = "babfialbi91-design/PAS-MAN-RELEASES";
    private static readonly HttpClient Http = CreateClient();

    public static string CurrentVersion
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly() ?? typeof(UpdateService).Assembly;
            return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PasswordManager-UpdateChecker/1.0");
        return client;
    }

    public static async Task<UpdateInfo?> CheckLatestAsync()
    {
        try
        {
            using var response = await Http.GetAsync($"https://api.github.com/repos/{Repo}/releases/latest");
            if (!response.IsSuccessStatusCode)
                return null;

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;

            var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? string.Empty : string.Empty;
            var htmlUrl = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? string.Empty : string.Empty;
            var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? string.Empty : string.Empty;
            var published = root.TryGetProperty("published_at", out var p) && p.TryGetDateTime(out var dt) ? dt : DateTime.MinValue;

            var assetUrl = string.Empty;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                        asset.TryGetProperty("browser_download_url", out var u))
                    {
                        assetUrl = u.GetString() ?? string.Empty;
                        break;
                    }
                }
            }

            return new UpdateInfo
            {
                Tag = tag,
                Version = tag.TrimStart('v', 'V'),
                DownloadUrl = assetUrl,
                ReleaseUrl = htmlUrl,
                Notes = notes,
                PublishedAt = published
            };
        }
        catch
        {
            return null;
        }
    }

    public static bool IsNewer(string latest, string current)
    {
        if (Version.TryParse(latest, out var latestVersion) && Version.TryParse(current, out var currentVersion))
            return latestVersion > currentVersion;
        return !string.IsNullOrEmpty(latest) && !string.Equals(latest, current, StringComparison.OrdinalIgnoreCase);
    }
}
