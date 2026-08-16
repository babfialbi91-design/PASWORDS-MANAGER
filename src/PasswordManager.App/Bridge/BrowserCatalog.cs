namespace PasswordManager.App.Bridge;

/// <summary>
/// كتالوج المتصفحات المدعومة في التطبيق.
/// </summary>
internal static class BrowserCatalog
{
    private static readonly BrowserInfo[] All =
    [
        new()
        {
            Id = "chrome",
            NameAr = "Google Chrome",
            NameEn = "Google Chrome",
            Logo = "assets/browsers/chrome.png",
            Executables = ["chrome.exe", "msedge.exe"],
            AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe",
            NativeHostRegKey = @"Software\Google\Chrome\NativeMessagingHosts",
            BrowserId = "chrome"
        },
        new()
        {
            Id = "edge",
            NameAr = "Microsoft Edge",
            NameEn = "Microsoft Edge",
            Logo = "assets/browsers/edge.png",
            Executables = ["msedge.exe", "chrome.exe"],
            AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe",
            NativeHostRegKey = @"Software\Microsoft\Edge\NativeMessagingHosts",
            BrowserId = "edge"
        },
        new()
        {
            Id = "brave",
            NameAr = "Brave",
            NameEn = "Brave",
            Logo = "assets/browsers/brave.png",
            Executables = ["brave.exe"],
            AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\brave.exe",
            NativeHostRegKey = @"Software\BraveSoftware\Brave\NativeMessagingHosts",
            BrowserId = "brave"
        },
        new()
        {
            Id = "opera",
            NameAr = "Opera",
            NameEn = "Opera",
            Logo = "assets/browsers/opera.png",
            Executables = ["opera.exe", "launcher.exe"],
            AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\opera.exe",
            NativeHostRegKey = @"Software\Opera Software\NativeMessagingHosts",
            BrowserId = "opera"
        },
        new()
        {
            Id = "operagx",
            NameAr = "Opera GX",
            NameEn = "Opera GX",
            Logo = "assets/browsers/operagx.png",
            Executables = ["opera.exe", "launcher.exe"],
            NativeHostRegKey = @"Software\Opera Software\NativeMessagingHosts",
            BrowserId = "operagx"
        },
        new()
        {
            Id = "vivaldi",
            NameAr = "Vivaldi",
            NameEn = "Vivaldi",
            Logo = "assets/browsers/vivaldi.png",
            Executables = ["vivaldi.exe"],
            AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\vivaldi.exe",
            NativeHostRegKey = @"Software\Vivaldi\NativeMessagingHosts",
            BrowserId = "vivaldi"
        },
        new()
        {
            Id = "chromium",
            NameAr = "Chromium",
            NameEn = "Chromium",
            Logo = "assets/browsers/chromium.png",
            Executables = ["chrome.exe", "msedge.exe"],
            NativeHostRegKey = @"Software\Chromium\NativeMessagingHosts",
            BrowserId = "chromium"
        },
        new()
        {
            Id = "firefox",
            NameAr = "Mozilla Firefox",
            NameEn = "Mozilla Firefox",
            Logo = "assets/browsers/firefox.png",
            Executables = ["firefox.exe"],
            AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe",
            BrowserId = "firefox"
        },
        new()
        {
            Id = "duckduckgo",
            NameAr = "DuckDuckGo",
            NameEn = "DuckDuckGo",
            Logo = "assets/browsers/duckduckgo.png",
            Executables = ["DuckDuckGo.exe"],
            BrowserId = "duckduckgo"
        }
    ];

    public static IReadOnlyList<BrowserInfo> Browsers => All;

    public static BrowserInfo? Find(string id)
        => Array.Find(All, b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));

    public static bool IsKnown(string id) => Find(id) is not null;
}
