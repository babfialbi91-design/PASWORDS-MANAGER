using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.App;

public partial class MainWindow : Window
{
    private VaultSession? _session;
    private string _activeView = "passwords";
    private string? _updateUrl;
    private string? _updateVersion;

    public MainWindow()
    {
        InitializeComponent();

        Localization.LanguageChanged += OnLanguageChanged;
        ApplyFlowDirection();

        var path = ResolveVaultPath();
        var vault = new VaultService(path);

        LoginView.Vault = vault;
        LoginView.IsSetupMode = !vault.Exists;
        LoginView.Unlocked += LoginView_Unlocked;

        VaultPathText.Text = path;

        Loaded += (_, _) => LoginView.FocusFirst();
    }

    private void OnLanguageChanged()
    {
        ApplyFlowDirection();
        if (UpdateBar.Visibility == Visibility.Visible && !string.IsNullOrEmpty(_updateVersion))
            UpdateBarText.Text = string.Format(Localization.Get("Update_BarNew"), _updateVersion);
    }

    private void ApplyFlowDirection()
        => FlowDirection = Localization.Instance.IsRtl ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

    private static string ResolveVaultPath()
    {
        var args = Environment.GetCommandLineArgs();
        var path = args.Length > 1
            ? Path.GetFullPath(args[1] ?? string.Empty)
            : Environment.GetEnvironmentVariable("PM_VAULT_PATH");

        if (string.IsNullOrEmpty(path))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(appData, "PasswordManager", "vault.dat");
        }

        return path;
    }

    private void LoginView_Unlocked(object? sender, EventArgs e)
    {
        _session = new VaultSession(LoginView.Vault!, LoginView.MasterPassword, LoginView.Data);
        LoginView.Visibility = Visibility.Collapsed;
        Dashboard.Visibility = Visibility.Visible;

        PasswordsView.Attach(_session);
        GeneratorView.Attach(_session);
        TotpView.Attach(_session);
        SettingsView.Attach(_session);

        ShowView("passwords");
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var info = await Task.Run(UpdateService.CheckLatestAsync);
            if (info is null || !UpdateService.IsNewer(info.Version, UpdateService.CurrentVersion))
                return;

            _updateVersion = info.Version;
            _updateUrl = string.IsNullOrEmpty(info.DownloadUrl) ? info.ReleaseUrl : info.DownloadUrl;
            UpdateBarText.Text = string.Format(Localization.Get("Update_BarNew"), info.Version);
            UpdateBar.Visibility = Visibility.Visible;
            SettingsView.SetUpdateAvailable(info.Version, _updateUrl);
        }
        catch
        {
            // تجاهل فشل فحص التحديثات
        }
    }

    private void UpdateDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_updateUrl) || string.IsNullOrEmpty(_updateVersion)) return;
        Dialogs.UpdateFlow.Start(this, _updateVersion, _updateUrl);
    }

    private void DismissUpdateBar_Click(object sender, RoutedEventArgs e)
    {
        UpdateBar.Visibility = Visibility.Collapsed;
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
            ShowView(tag);
    }

    private void ShowView(string tag)
    {
        _activeView = tag;
        PasswordsView.Visibility = tag == "passwords" ? Visibility.Visible : Visibility.Collapsed;
        GeneratorView.Visibility = tag == "generator" ? Visibility.Visible : Visibility.Collapsed;
        TotpView.Visibility = tag == "totp" ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = tag == "settings" ? Visibility.Visible : Visibility.Collapsed;

        SetNavState(tag);
        RefreshActiveView();
    }

    private void SetNavState(string activeTag)
    {
        NavPasswords.Background = activeTag == "passwords" ? (System.Windows.Media.Brush)FindResource("SurfaceAltBrush") : System.Windows.Media.Brushes.Transparent;
        NavGenerator.Background = activeTag == "generator" ? (System.Windows.Media.Brush)FindResource("SurfaceAltBrush") : System.Windows.Media.Brushes.Transparent;
        NavTotp.Background = activeTag == "totp" ? (System.Windows.Media.Brush)FindResource("SurfaceAltBrush") : System.Windows.Media.Brushes.Transparent;
        NavSettings.Background = activeTag == "settings" ? (System.Windows.Media.Brush)FindResource("SurfaceAltBrush") : System.Windows.Media.Brushes.Transparent;
    }

    private void RefreshActiveView()
    {
        switch (_activeView)
        {
            case "passwords": PasswordsView.Refresh(); break;
            case "totp": TotpView.Refresh(); break;
        }
    }

    public void Notify(string message)
    {
        VaultStatusText.Text = message;
        VaultStatusText.Foreground = (System.Windows.Media.Brush)FindResource("SuccessBrush");
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        timer.Tick += (_, _) =>
        {
            VaultStatusText.Text = Localization.Get("Sidebar_VaultOpen");
            timer.Stop();
        };
        timer.Start();
    }

    public void LockAndReset()
    {
        LockButton_Click(this, new RoutedEventArgs());
        LoginView.ResetForSetup();
    }

    private void LockButton_Click(object sender, RoutedEventArgs e)
    {
        _session = null;
        PasswordsView.Detach();
        GeneratorView.Detach();
        TotpView.Detach();
        SettingsView.Detach();

        Dashboard.Visibility = Visibility.Collapsed;
        LoginView.Visibility = Visibility.Visible;
        LoginView.ResetForUnlock();
    }

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_session is not null)
        {
            try { await _session.SaveAsync(); }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    string.Format(Localization.Get("Main_SaveError"), ex.Message),
                    Localization.Get("Common_Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        base.OnClosing(e);
    }
}
