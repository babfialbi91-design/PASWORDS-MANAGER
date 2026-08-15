using System.IO;
using System.Text.Json;

namespace PasswordManager.App;

public sealed class AppSettings
{
    public Language Language { get; set; } = Language.Arabic;

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
