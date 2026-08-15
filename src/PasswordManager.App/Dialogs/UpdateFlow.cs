using System.Diagnostics;
using System.Windows;
using PasswordManager.Services;

namespace PasswordManager.App.Dialogs;

/// <summary>
/// تدفق التحديث الذاتي: تحميل داخل التطبيق، ثم سؤال المستخدم عن إعادة التشغيل،
/// ثم التثبيت الصامت وإعادة تشغيل التطبيق تلقائياً.
/// </summary>
public static class UpdateFlow
{
    public static void Start(Window owner, string version, string downloadUrl)
    {
        if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(version))
            return;

        var installerPath = UpdatePaths.InstallerPath(version);
        var dialog = new UpdateDialog(owner, version, downloadUrl, installerPath);
        dialog.ShowDialog();

        if (!dialog.RestartNow)
            return;

        RestartToApply(installerPath);
    }

    private static void RestartToApply(string installerPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(installerPath)
            {
                UseShellExecute = false,
                Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS"
            });
        }
        catch
        {
            try { Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true }); }
            catch { /* تجاهل */ }
        }

        Application.Current.Shutdown();
    }
}
