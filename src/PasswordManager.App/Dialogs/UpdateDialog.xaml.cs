using System.IO;
using System.Windows;
using System.Windows.Controls;
using PasswordManager.Services;

namespace PasswordManager.App.Dialogs;

public partial class UpdateDialog : Window
{
    private readonly string _version;
    private readonly string _downloadUrl;
    private readonly string _installerPath;

    public bool RestartNow { get; private set; }

    public UpdateDialog(Window owner, string version, string downloadUrl, string installerPath)
    {
        InitializeComponent();
        Owner = owner;
        _version = version;
        _downloadUrl = downloadUrl;
        _installerPath = installerPath;

        // التحميل مكتمل مسبقاً من جلسة سابقة → انتقل مباشرة لطلب إعادة التشغيل
        if (File.Exists(_installerPath) && new FileInfo(_installerPath).Length > 1024 * 1024)
        {
            ShowReadyState();
            return;
        }

        ShowDownloadingState();
    }

    private void ShowDownloadingState()
    {
        DialogTitle.Text = string.Format(Localization.Get("Update_Downloading"), _version);
        DialogMessage.Text = Localization.Get("Update_DownloadingMsg");
        ProgressPanel.Visibility = Visibility.Visible;
        ButtonsPanel.Visibility = Visibility.Collapsed;
        DownloadProgress.Value = 0;
        PercentText.Text = "0%";
        SizeText.Text = string.Empty;

        _ = DownloadAsync();
    }

    private async Task DownloadAsync()
    {
        var progress = new Progress<double>(p =>
        {
            DownloadProgress.Value = Math.Clamp(p * 100, 0, 100);
            PercentText.Text = $"{Math.Min(99, (int)(p * 100))}%";
        });

        try
        {
            var tmp = _installerPath + ".part";
            if (File.Exists(tmp))
                File.Delete(tmp);

            await Task.Run(() => UpdateService.DownloadAsync(_downloadUrl, tmp, progress));

            File.Move(tmp, _installerPath, overwrite: true);
            ShowReadyState();
        }
        catch
        {
            ShowErrorState();
        }
    }

    private void ShowReadyState()
    {
        DialogTitle.Text = string.Format(Localization.Get("Update_Ready"), _version);
        DialogMessage.Text = Localization.Get("Update_ReadyMsg");
        ProgressPanel.Visibility = Visibility.Collapsed;
        ButtonsPanel.Visibility = Visibility.Visible;
        RestartNowButton.Content = Localization.Get("Update_RestartNow");
        LaterButton.Content = Localization.Get("Update_Later");
        LaterButton.Focus();
    }

    private void ShowErrorState()
    {
        DialogTitle.Text = Localization.Get("Update_DownloadFailed");
        DialogMessage.Text = Localization.Get("Update_DownloadFailedMsg");
        ProgressPanel.Visibility = Visibility.Collapsed;
        ButtonsPanel.Visibility = Visibility.Visible;
        RestartNowButton.Visibility = Visibility.Collapsed;
        LaterButton.Visibility = Visibility.Collapsed;
        CloseButton.Visibility = Visibility.Visible;
        CloseButton.Content = Localization.Get("Common_Close");
        CloseButton.Focus();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void RestartNowButton_Click(object sender, RoutedEventArgs e)
    {
        RestartNow = true;
        DialogResult = true;
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
