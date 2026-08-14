using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PasswordManager.App.Views;

public partial class SettingsView : UserControl
{
    private VaultSession? _session;

    public SettingsView()
    {
        InitializeComponent();
    }

    public void Attach(VaultSession session)
    {
        _session = session;
        VaultPathText.Text = session.VaultPath;
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
