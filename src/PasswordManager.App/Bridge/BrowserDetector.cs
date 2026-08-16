using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PasswordManager.App.Bridge;

/// <summary>
/// كشف مسار تنفيذ المتصفح عبر السجل (App Paths) أو المسارات المعروفة.
/// </summary>
internal static class BrowserDetector
{
    /// <summary>يعيد مسار exe للمتصفح أو null إن لم يُعثر عليه.</summary>
    public static string? FindExecutable(BrowserInfo browser)
    {
        foreach (var exe in browser.Executables)
        {
            var fromAppPaths = FindViaAppPaths(exe);
            if (!string.IsNullOrEmpty(fromAppPaths) && File.Exists(fromAppPaths))
                return fromAppPaths;
        }

        foreach (var exe in browser.Executables)
        {
            var fromLocalAppData = FindInCommonDirs(exe);
            if (!string.IsNullOrEmpty(fromLocalAppData) && File.Exists(fromLocalAppData))
                return fromLocalAppData;
        }

        return null;
    }

    private static string? FindViaAppPaths(string exe)
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\App Paths\{exe}");
            var path = key?.GetValue(null) as string;
            key?.Dispose();
            if (!string.IsNullOrWhiteSpace(path)) return path.Trim();

            key = Registry.LocalMachine.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\App Paths\{exe}");
            path = key?.GetValue(null) as string;
            key?.Dispose();
            return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static string? FindInCommonDirs(string exe)
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        foreach (var root in roots)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
            try
            {
                var match = Directory
                    .EnumerateFiles(root, exe, SearchOption.AllDirectories)
                    .FirstOrDefault(IsRunnable);
                if (match is not null) return match;
            }
            catch
            {
                // تجاهل مجلدات غير قابلة للقراءة
            }
        }

        return null;
    }

    private static bool IsRunnable(string path)
    {
        try
        {
            var lower = path.ToLowerInvariant();
            return lower.Contains("microsoft") || lower.Contains("google") || lower.Contains("brave")
                || lower.Contains("opera") || lower.Contains("vivaldi") || lower.Contains("firefox")
                || lower.Contains("chromium") || lower.Contains("duckduckgo");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>يتحقق إن كانت السجلات ما زالت تسجّل المضيف لهذا المتصفح.</summary>
    public static bool IsRegistered(BrowserInfo browser, string manifestPath)
    {
        if (string.IsNullOrEmpty(browser.NativeHostRegKey)) return false;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(browser.NativeHostRegKey);
            var value = key?.GetValue(BridgeConstants.HostName) as string;
            return string.Equals(value, manifestPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
