using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PasswordManager.App.Bridge;
using PasswordManager.Services;

namespace PasswordManager.App.Views;

public partial class BrowsersView : UserControl
{
    private HotkeyManager? _hotkeyManager;
    private VaultSession? _session;

    /// <summary>يُستدعى عندما يتغير الاختصار أو تفعيل جسر الكتابة.</summary>
    public event Action? HotkeyChanged;

    public BrowsersView()
    {
        InitializeComponent();
    }

    public void Attach(VaultSession session)
    {
        _session = session;
        Refresh();
    }

    public void Detach()
    {
        _session = null;
    }

    public void Refresh()
    {
        RebuildCards();
        RebuildHotkeyUi();
    }

    private void RebuildHotkeyUi()
    {
        var settings = AppSettings.Load();
        TypingEnabledCheck.IsChecked = settings.TypingBridgeEnabled;
        HotkeyText.Text = HotkeyManager.Describe(settings.HotkeyModifiers, settings.HotkeyKey);
    }

    private void RebuildCards()
    {
        CardsPanel.Children.Clear();
        var settings = AppSettings.Load();

        foreach (var browser in BrowserCatalog.Browsers)
        {
            var linked = settings.LinkedBrowsers.FirstOrDefault(l => l.Id == browser.Id);
            CardsPanel.Children.Add(BuildCard(browser, linked));
        }
    }

    private Border BuildCard(BrowserInfo browser, LinkedBrowser? linked)
    {
        var card = new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderBrush = (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Width = 158,
            Margin = new Thickness(0, 0, 12, 12),
            Padding = new Thickness(14, 14, 14, 12)
        };

        var stack = new StackPanel();

        var logo = new Image
        {
            Source = LoadLogo(browser.Logo),
            Width = 42,
            Height = 42,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(logo);

        stack.Children.Add(new TextBlock
        {
            Text = browser.DisplayName,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        });

        var status = new TextBlock
        {
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };

        if (linked is not null)
        {
            status.Text = Localization.Get("Bridge_Linked");
            status.Foreground = (Brush)FindResource("SuccessBrush");
        }
        else if (!browser.SupportsExtension)
        {
            status.Text = Localization.Get("Bridge_NotSupported");
            status.Foreground = (Brush)FindResource("WarningBrush");
        }
        else
        {
            status.Text = Localization.Get("Bridge_NotLinked");
            status.Foreground = (Brush)FindResource("TextMutedBrush");
        }
        stack.Children.Add(status);

        var button = new Button
        {
            Margin = new Thickness(0, 10, 0, 0),
            Padding = new Thickness(10, 6, 10, 6),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (linked is not null)
        {
            button.Content = Localization.Get("Bridge_UnlinkBtn");
            button.Background = Brushes.Transparent;
            button.BorderBrush = (Brush)FindResource("DangerBrush");
            button.Foreground = (Brush)FindResource("DangerBrush");
            button.Click += (_, _) => Unlink(browser);
        }
        else
        {
            button.Content = Localization.Get("Bridge_LinkBtn");
            button.SetResourceReference(BackgroundProperty, "AccentBrush");
            button.SetResourceReference(BorderBrushProperty, "AccentBrush");
            button.SetResourceReference(ForegroundProperty, "TextBrush");
            button.Click += (_, _) => Link(browser);
        }

        stack.Children.Add(button);
        card.Child = stack;
        return card;
    }

    private static BitmapImage LoadLogo(string logoPath)
    {
        var uri = new Uri($"pack://application:,,,/PasswordManager;component/{logoPath}", UriKind.Absolute);
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = uri;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        return image;
    }

    private void Link(BrowserInfo browser)
    {
        StatusMessage.Visibility = Visibility.Collapsed;

        var exe = BrowserDetector.FindExecutable(browser);
        if (exe is null)
        {
            ShowStatus(Localization.Get("Bridge_DetectFailed"), "DangerBrush");
            return;
        }

        var ok = true;
        if (browser.SupportsExtension)
        {
            ok = ExtensionGenerator.Generate() && NativeMessagingHost.Install(browser);
            if (ok) ShowInstructions(browser);
        }

        if (!ok)
        {
            ShowStatus(Localization.Get("Bridge_LinkFailed"), "DangerBrush");
            return;
        }

        var settings = AppSettings.Load();
        settings.LinkedBrowsers.RemoveAll(l => l.Id == browser.Id);
        settings.LinkedBrowsers.Add(new LinkedBrowser
        {
            Id = browser.Id,
            Path = exe,
            Method = browser.SupportsExtension ? BridgeMethod.Extension : BridgeMethod.Typing
        });
        settings.Save();

        RebuildCards();
        ShowStatus(browser.SupportsExtension ? Localization.Get("Bridge_LinkOk") : Localization.Get("Bridge_TypeOnlyOk"), "SuccessBrush");
    }

    private void Unlink(BrowserInfo browser)
    {
        var settings = AppSettings.Load();
        settings.LinkedBrowsers.RemoveAll(l => l.Id == browser.Id);
        if (!string.IsNullOrEmpty(browser.NativeHostRegKey))
            NativeMessagingHost.Uninstall(browser);
        settings.Save();

        InstructionPanel.Visibility = Visibility.Collapsed;
        RebuildCards();
    }

    private void ShowInstructions(BrowserInfo browser)
    {
        var extensionsUrl = browser.Id switch
        {
            "edge" => "edge://extensions",
            "brave" => "brave://extensions",
            "opera" or "operagx" => "opera://extensions",
            "vivaldi" => "vivaldi://extensions",
            _ => "chrome://extensions"
        };

        InstructionTitle.Text = string.Format(Localization.Get("Bridge_ExtTitle"), browser.DisplayName);
        InstructionText.Text = string.Format(Localization.Get("Bridge_ExtSteps"), extensionsUrl, BridgeConstants.ExtensionDir);
        InstructionPanel.Visibility = Visibility.Visible;
    }

    private void ShowStatus(string message, string brushKey)
    {
        StatusMessage.Text = message;
        StatusMessage.Foreground = (Brush)FindResource(brushKey);
        StatusMessage.Visibility = Visibility.Visible;
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(BridgeConstants.ExtensionDir);
            Process.Start("explorer.exe", $"\"{BridgeConstants.ExtensionDir}\"");
        }
        catch
        {
            // تجاهل
        }
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        SecureClipboard.SetText(BridgeConstants.ExtensionDir);
    }

    private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
    {
        _hotkeyManager ??= new HotkeyManager();
        CaptureTip.Text = Localization.Get("Bridge_HotkeyCaptureTip");
        CaptureTip.Visibility = Visibility.Visible;
        _hotkeyManager.Capture(OnCaptured);
    }

    private void OnCaptured(string combo)
    {
        CaptureTip.Visibility = Visibility.Collapsed;
        if (string.IsNullOrEmpty(combo)) return;

        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var key = parts[^1];
        var modifiers = string.Join(",", parts.Take(parts.Length - 1));

        var settings = AppSettings.Load();
        settings.HotkeyModifiers = modifiers;
        settings.HotkeyKey = key;
        settings.Save();

        RebuildHotkeyUi();
        HotkeyChanged?.Invoke();
    }

    private void TypingEnabled_Changed(object sender, RoutedEventArgs e)
    {
        var settings = AppSettings.Load();
        settings.TypingBridgeEnabled = TypingEnabledCheck.IsChecked == true;
        settings.Save();
        HotkeyChanged?.Invoke();
    }
}
