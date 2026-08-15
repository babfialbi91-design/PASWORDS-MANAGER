using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PasswordManager.Services;

namespace PasswordManager.App.Views;

public partial class SettingsView : UserControl
{
    private VaultSession? _session;
    private string? _updateUrl;

    public SettingsView()
    {
        InitializeComponent();
    }

    public void Attach(VaultSession session)
    {
        _session = session;
        VaultPathText.Text = session.VaultPath;
        CurrentVersionText.Text = $"الإصدار الحالي: {UpdateService.CurrentVersion}";
        AboutVersionText.Text = $"مدير كلمات المرور — الإصدار {UpdateService.CurrentVersion}";
    }

    public void Detach()
    {
        _session = null;
    }

    public void Refresh()
    {
        if (_session is not null)
            VaultPathText.Text = _session.VaultPath;
    }

    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null) return;

        var oldPw = CurrentPasswordBox.Password;
        var newPw = NewPasswordBox.Password;
        var confirm = ConfirmPasswordBox.Password;

        ChangeMessage.Visibility = Visibility.Collapsed;

        try
        {
            // التحقق من كلمة المرور الحالية
            var check = await _session.Vault.OpenAsync(oldPw);
        }
        catch (CryptographicException)
        {
            ShowChangeError("كلمة المرور الحالية غير صحيحة.");
            return;
        }

        if (newPw.Length < 8)
        {
            ShowChangeError("كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل.");
            return;
        }
        if (newPw != confirm)
        {
            ShowChangeError("كلمتا المرور الجديدتان غير متطابقتين.");
            return;
        }

        await _session.Vault.ChangeMasterPasswordAsync(oldPw, newPw);
        _session.MasterPassword = newPw;

        CurrentPasswordBox.Clear();
        NewPasswordBox.Clear();
        ConfirmPasswordBox.Clear();

        ChangeMessage.Text = "✓ تم تغيير كلمة المرور الرئيسية بنجاح.";
        ChangeMessage.Foreground = (Brush)FindResource("SuccessBrush");
        ChangeMessage.Visibility = Visibility.Visible;
    }

    private void ShowChangeError(string message)
    {
        ChangeMessage.Text = $"✗ {message}";
        ChangeMessage.Foreground = (Brush)FindResource("DangerBrush");
        ChangeMessage.Visibility = Visibility.Visible;
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_session?.VaultPath ?? string.Empty);
        }
        catch
        {
            // تجاهل
        }
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatusText.Text = "جاري التحقق من التحديثات...";
        UpdateStatusText.Foreground = (Brush)FindResource("TextMutedBrush");
        DownloadUpdateButton.Visibility = Visibility.Collapsed;

        var info = await Task.Run(UpdateService.CheckLatestAsync);

        if (info is null)
        {
            UpdateStatusText.Text = "تعذّر الاتصال بالخادم. تأكد من اتصالك بالإنترنت.";
            UpdateStatusText.Foreground = (Brush)FindResource("DangerBrush");
            return;
        }

        if (UpdateService.IsNewer(info.Version, UpdateService.CurrentVersion))
        {
            _updateUrl = string.IsNullOrEmpty(info.DownloadUrl) ? info.ReleaseUrl : info.DownloadUrl;
            UpdateStatusText.Text = $"يتوفر تحديث جديد: الإصدار {info.Version}";
            UpdateStatusText.Foreground = (Brush)FindResource("WarningBrush");
            DownloadUpdateButton.Visibility = Visibility.Visible;
        }
        else
        {
            UpdateStatusText.Text = "أنت على أحدث إصدار ✅";
            UpdateStatusText.Foreground = (Brush)FindResource("SuccessBrush");
        }
    }

    public void SetUpdateAvailable(string version, string url)
    {
        _updateUrl = url;
        UpdateStatusText.Text = $"يتوفر تحديث جديد: الإصدار {version}";
        UpdateStatusText.Foreground = (Brush)FindResource("WarningBrush");
        DownloadUpdateButton.Visibility = Visibility.Visible;
    }

    private void DownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_updateUrl)) return;
        try { Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true }); }
        catch { /* تجاهل */ }
    }

    private void CreateShortcut_Click(object sender, RoutedEventArgs e)
    {
        ShortcutMessage.Visibility = Visibility.Collapsed;
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = System.IO.Path.Combine(desktop, "مدير كلمات المرور.lnk");

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
                throw new InvalidOperationException("WScript.Shell غير متوفر.");

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exe;
            shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(exe) ?? string.Empty;
            shortcut.IconLocation = $"{exe},0";
            shortcut.Description = "مدير كلمات المرور";
            shortcut.Save();

            ShortcutMessage.Text = "✓ تم إنشاء الاختصار على سطح المكتب.";
            ShortcutMessage.Foreground = (Brush)FindResource("SuccessBrush");
            ShortcutMessage.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShortcutMessage.Text = $"✗ فشل الإنشاء: {ex.Message}";
            ShortcutMessage.Foreground = (Brush)FindResource("DangerBrush");
            ShortcutMessage.Visibility = Visibility.Visible;
        }
    }

    private async void ResetVault_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null) return;

        var result = MessageBox.Show(
            "تحذير: سيتم حذف كل كلمات المرور وحسابات TOTP نهائياً ولا يمكن استرجاعها.\nهل أنت متأكد تماماً؟",
            "إعادة تعيين الخزنة",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        await Task.Run(() =>
        {
            try { _session.Vault.Reset(); }
            catch { /* تجاهل */ }
        });

        var window = Window.GetWindow(this) as MainWindow;
        window?.LockAndReset();
    }
}
