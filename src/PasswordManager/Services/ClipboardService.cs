using System.Diagnostics;

namespace PasswordManager.Services;

/// <summary>
/// نسخ نص إلى الحافظة عبر clip.exe (ويندوز) مع التنظيف بعد النسخ.
/// </summary>
public static class ClipboardService
{
    public static bool TryCopy(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "clip.exe",
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                if (!process.Start())
                    return false;

                process.StandardInput.Write(text);
                process.StandardInput.Close();
                process.WaitForExit(3000);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
