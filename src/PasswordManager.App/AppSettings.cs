using System.IO;
using System.Text.Json;
using PasswordManager.App.Bridge;

namespace PasswordManager.App;

public sealed class AppSettings
{
    public Language Language { get; set; } = Language.Arabic;

    public int AutoLockMinutes { get; set; } = 5;

    /// <summary>المتصفحات المرتبطة عبر الجسر.</summary>
    public List<LinkedBrowser> LinkedBrowsers { get; set; } = new();

    /// <summary>تفعيل جسر الكتابة (اختصار عام).</summary>
    public bool TypingBridgeEnabled { get; set; } = true;

    /// <summary>اختصار لوحة الكتابة — مفاتيح التعديل (Control,Alt,Shift,Win).</summary>
    public string HotkeyModifiers { get; set; } = "Control,Shift";

    /// <summary>مفتاح الاختصار (اسم لوحة المفاتيح الافتراضي).</summary>
    public string HotkeyKey { get; set; } = "L";

    private static string SettingsPath
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasswordManager", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this));
        }
        catch
        {
            // تجاهل فشل الحفظ — لا يجب أن يمنع تغيير اللغة
        }
    }
}
