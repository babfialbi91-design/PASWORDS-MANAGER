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
    private string? _updateVersion;
    private string? _statusKey;
    private object?[]? _statusArgs;

    private static readonly (int Minutes, string Key)[] AutoLockOptions =
    {
        (0, "Settings_AutoLockNever"),
        (1, "Settings_AutoLock1"),
        (5, "Settings_AutoLock5"),
        (10, "Settings_AutoLock10"),
        (30, "Settings_AutoLock30")
    };

    public SettingsView()
    {
        InitializeComponent();

        LanguageBox.Items.Add("العربية");
        LanguageBox.Items.Add("English");
        LanguageBox.SelectedIndex = Localization.Instance.IsRtl ? 0 : 1;

        PopulateAutoLock();
        Localization.LanguageChanged += ApplyLanguage;
    }

    private void PopulateAutoLock()
    {
        var current = AppSettings.Load().AutoLockMinutes;
        AutoLockBox.Items.Clear();
        var index = 0;
        for (var i = 0; i < AutoLockOptions.Length; i++)
        {
            var option = AutoLockOptions[i];
            AutoLockBox.Items.Add(new ComboBoxItem { Content = Localization.Get(option.Key), Tag = option.Minutes });
            if (option.Minutes == current) index = i;
        }
        AutoLockBox.SelectedIndex = index;
    }

    private void AutoLockBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AutoLockBox.SelectedItem is not ComboBoxItem { Tag: int minutes }) return;
        var settings = AppSettings.Load();
        settings.AutoLockMinutes = minutes;
        settings.Save();
    }

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageBox.SelectedIndex < 0) return;
        Localization.Instance.Language = LanguageBox.SelectedIndex == 0 ? PasswordManager.App.Language.Arabic : PasswordManager.App.Language.English;
    }

    public void Attach(VaultSession session)
    {
        _session = session;
        VaultPathText.Text = session.VaultPath;
        ApplyLanguage();
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

    private void ApplyLanguage()
    {
        CurrentVersionText.Text = string.Format(Localization.Get("Settings_CurrentVersion"), UpdateService.CurrentVersion);
        AboutVersionText.Text = string.Format(Localization.Get("Settings_About"), UpdateService.CurrentVersion);
        PopulateAutoLock();
        if (_statusKey is not null)
            RenderUpdateStatus(_statusKey, _statusArgs);
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
            ShowChangeError(Localization.Get("Settings_ChangeErrCurrent"));
            return;
        }

        if (newPw.Length < 8)
        {
            ShowChangeError(Localization.Get("Settings_ChangeErrTooShort"));
            return;
        }
        if (newPw != confirm)
        {
            ShowChangeError(Localization.Get("Settings_ChangeErrMismatch"));
            return;
        }

        await _session.Vault.ChangeMasterPasswordAsync(oldPw, newPw);
        _session.MasterPassword = newPw;

        CurrentPasswordBox.Clear();
        NewPasswordBox.Clear();
        ConfirmPasswordBox.Clear();

        ChangeMessage.Text = Localization.Get("Settings_ChangeOk");
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
        SecureClipboard.SetText(_session?.VaultPath ?? string.Empty);
    }

    private void RenderUpdateStatus(string key, object?[]? args)
    {
        _statusKey = key;
        _statusArgs = args;
        UpdateStatusText.Text = args is { Length: > 0 } ? string.Format(Localization.Get(key), args) : Localization.Get(key);

        UpdateStatusText.Foreground = key switch
        {
            "Settings_NewAvailable" => (Brush)FindResource("WarningBrush"),
            "Settings_UpToDate" => (Brush)FindResource("SuccessBrush"),
            "Settings_ConnFailed" => (Brush)FindResource("DangerBrush"),
            _ => (Brush)FindResource("TextMutedBrush")
        };
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        RenderUpdateStatus("Settings_Checking", null);
        DownloadUpdateButton.Visibility = Visibility.Collapsed;

        var info = await Task.Run(UpdateService.CheckLatestAsync);

        if (info is null)
        {
            RenderUpdateStatus("Settings_ConnFailed", null);
            return;
        }

        if (UpdateService.IsNewer(info.Version, UpdateService.CurrentVersion))
        {
            _updateVersion = info.Version;
            _updateUrl = string.IsNullOrEmpty(info.DownloadUrl) ? info.ReleaseUrl : info.DownloadUrl;
            RenderUpdateStatus("Settings_NewAvailable", new object[] { info.Version });
            DownloadUpdateButton.Visibility = Visibility.Visible;
        }
        else
        {
            RenderUpdateStatus("Settings_UpToDate", null);
        }
    }

    public void SetUpdateAvailable(string version, string url)
    {
        _updateVersion = version;
        _updateUrl = url;
        RenderUpdateStatus("Settings_NewAvailable", new object[] { version });
        DownloadUpdateButton.Visibility = Visibility.Visible;
    }

    private void DownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_updateUrl) || string.IsNullOrEmpty(_updateVersion)) return;
        var window = Window.GetWindow(this);
        if (window is null) return;
        Dialogs.UpdateFlow.Start(window, _updateVersion, _updateUrl);
    }

    private void CreateShortcut_Click(object sender, RoutedEventArgs e)
    {
        ShortcutMessage.Visibility = Visibility.Collapsed;
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = System.IO.Path.Combine(desktop, Localization.Instance.IsRtl ? "مدير كلمات المرور.lnk" : "Password Manager.lnk");

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
                throw new InvalidOperationException("WScript.Shell");

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exe;
            shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(exe) ?? string.Empty;
            shortcut.IconLocation = $"{exe},0";
            shortcut.Description = Localization.Get("App_Title");
            shortcut.Save();

            ShortcutMessage.Text = Localization.Get("Settings_ShortcutOk");
            ShortcutMessage.Foreground = (Brush)FindResource("SuccessBrush");
            ShortcutMessage.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShortcutMessage.Text = string.Format(Localization.Get("Settings_ShortcutFail"), ex.Message);
            ShortcutMessage.Foreground = (Brush)FindResource("DangerBrush");
            ShortcutMessage.Visibility = Visibility.Visible;
        }
    }

    private async void ResetVault_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null) return;

        var result = MessageBox.Show(
            Localization.Get("Settings_ResetConfirmMsg"),
            Localization.Get("Login_ResetConfirmTitle"),
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
