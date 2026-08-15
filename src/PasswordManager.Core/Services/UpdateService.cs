using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PasswordManager.Services;

public static class UpdatePaths
{
    public static string DownloadFolder
    {
        get
        {
            var folder = Path.Combine(Path.GetTempPath(), "PasswordManagerUpdate");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public static string InstallerPath(string version)
        => Path.Combine(DownloadFolder, $"PasswordManagerSetup-{version}.exe");
}

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
    private static readonly HttpClient DownloadClient = CreateDownloadClient();

    /// <summary>
    /// يسمح فقط بتنزيل المثبّت من GitHub — يمنع أي عنوان آخر من تنفيذ تحميلات تعسفية.
    /// </summary>
    public static bool IsTrustedDownloadUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
        return uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// يتحقق أن الإصدار رقم فقط (مثال: 1.2.0) — يستخدم داخل اسم ملف المثبّت، لذا يمنع أي محاولة حقن مسار.
    /// </summary>
    public static bool IsValidVersion(string? version)
        => !string.IsNullOrWhiteSpace(version) && Regex.IsMatch(version, @"^\d+(\.\d+){1,3}$");

    public static string CurrentVersion
    {
        get
        {
            var forced = Environment.GetEnvironmentVariable("PM_FORCE_VERSION");
            if (!string.IsNullOrWhiteSpace(forced))
                return forced;

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

    private static HttpClient CreateDownloadClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PasswordManager-Updater/1.0");
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

    public static async Task DownloadAsync(string url, string destPath, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsTrustedDownloadUrl(url))
            throw new InvalidOperationException("Untrusted download URL.");

        using var response = await DownloadClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(destPath);

        var buffer = new byte[81920];
        long read = 0;
        int bytes;
        while ((bytes = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, bytes), cancellationToken);
            read += bytes;
            if (total > 0)
                progress?.Report((double)read / total);
        }
    }
}
