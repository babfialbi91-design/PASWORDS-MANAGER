using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace PasswordManager.App.Bridge;

/// <summary>
/// كتابة ملف تعريف المضيف الأصلي وتسجيله في سجل المتصفح (HKCU).
/// </summary>
internal static class NativeMessagingHost
{
    /// <summary>يعيد مسار ملف التعريف الخاص بمتصفح معين.</summary>
    public static string ManifestPath(BrowserInfo browser)
        => Path.Combine(BridgeConstants.HostManifestsDir, $"{BridgeConstants.HostName}.{browser.Id}.json");

    /// <summary>
    /// يكتب ملف التعريف ويسجل القيمة في سجل المتصفح.
    /// يعيد true عند النجاح.
    /// </summary>
    public static bool Install(BrowserInfo browser)
    {
        try
        {
            Directory.CreateDirectory(BridgeConstants.HostManifestsDir);
            var manifest = new Dictionary<string, object?>
            {
                ["name"] = BridgeConstants.HostName,
                ["description"] = "Password Manager native messaging bridge",
                ["path"] = Environment.ProcessPath ?? string.Empty,
                ["type"] = "stdio",
                ["allowed_origins"] = new[] { $"chrome-extension://{BridgeConstants.ExtensionId}/" }
            };

            File.WriteAllText(
                ManifestPath(browser),
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            if (string.IsNullOrEmpty(browser.NativeHostRegKey))
                return false;

            using var key = Registry.CurrentUser.CreateSubKey(browser.NativeHostRegKey);
            key?.SetValue(BridgeConstants.HostName, ManifestPath(browser));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>إزالة قيمة السجل الخاصة بالمتصفح (دون حذف ملفات الامتداد المشتركة).</summary>
    public static void Uninstall(BrowserInfo browser)
    {
        if (string.IsNullOrEmpty(browser.NativeHostRegKey)) return;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(browser.NativeHostRegKey, writable: true);
            key?.DeleteValue(BridgeConstants.HostName, throwOnMissingValue: false);
        }
        catch
        {
            // تجاهل
        }
    }
}
