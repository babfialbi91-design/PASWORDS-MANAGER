using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PasswordManager.App.Bridge;
using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.App.Dialogs;

public partial class FillConfirmWindow : Window
{
    private readonly List<PasswordEntry> _entries = new();
    private readonly List<TotpAccount> _allTotp = new();
    private readonly Dictionary<PasswordEntry, RadioButton> _entryButtons = new();
    private readonly Dictionary<TotpAccount, ComboBoxItem> _totpItems = new();

    private PasswordEntry? _selectedEntry;
    private readonly DispatcherTimer _totpTimer;
    private bool _typingMode;

    /// <summary>قرار التعبئة عند النجاح (يُقرأ بعد إغلاق النافذة).</summary>
    public FillDecision? Result { get; private set; }

    public FillConfirmWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            FlowDirection = Localization.Instance.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            RefreshPreview();
        };
        _totpTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _totpTimer.Tick += (_, _) => RefreshPreview();
    }

    /// <summary>
    /// تجهيز النافذة. في وضع الامتداد تُمرَّر الحسابات المطابقة للموقع،
    /// وفي وضع الكتابة تُمرَّر كل الحسابات ويُعرض حقل بحث.
    /// </summary>
    public void Setup(
        bool typingMode,
        IReadOnlyList<PasswordEntry> entries,
        IReadOnlyList<TotpAccount> allTotp,
        string? siteUrl = null,
        string? siteTitle = null)
    {
        _typingMode = typingMode;
        _entries.Clear();
        _entries.AddRange(entries);
        _allTotp.Clear();
        _allTotp.AddRange(allTotp);

        if (typingMode)
        {
            SiteTitleText.Text = Localization.Get("Bridge_PaletteTitle");
            SiteUrlText.Text = Localization.Get("Bridge_PaletteSub");
            SearchBox.Visibility = Visibility.Visible;
            FillButton.Content = Localization.Get("Bridge_TypeNow");
        }
        else
        {
            SiteTitleText.Text = string.IsNullOrWhiteSpace(siteTitle)
                ? Localization.Get("Bridge_FillRequestTitle")
                : siteTitle;
            SiteUrlText.Text = siteUrl ?? string.Empty;
            SearchBox.Visibility = Visibility.Collapsed;
        }

        BuildEntryList();
        UpdateTotpPanel();
    }

    private void BuildEntryList()
    {
        EntriesPanel.Children.Clear();
        _entryButtons.Clear();

        if (_entries.Count == 0)
        {
            NoMatchText.Text = _typingMode
                ? Localization.Get("Pass_Empty")
                : Localization.Get("Bridge_NoMatch");
            NoMatchText.Visibility = Visibility.Visible;
            FillButton.IsEnabled = false;
            FillUsernameCheck.IsEnabled = false;
            FillPasswordCheck.IsEnabled = false;
            return;
        }

        NoMatchText.Visibility = Visibility.Collapsed;
        FillButton.IsEnabled = true;
        FillUsernameCheck.IsEnabled = true;
        FillPasswordCheck.IsEnabled = true;

        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var radio = new RadioButton
            {
                GroupName = "entries",
                Tag = entry,
                IsChecked = i == 0,
                Margin = new Thickness(0, 4, 0, 4),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var content = new StackPanel();
            var title = new TextBlock { Text = entry.Title, FontWeight = FontWeights.SemiBold, FontSize = 13 };
            var sub = new TextBlock
            {
                Text = $"{entry.Username}  ·  {entry.Website}",
                FontSize = 11,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Margin = new Thickness(22, 2, 0, 0)
            };
            content.Children.Add(title);
            content.Children.Add(sub);
            radio.Content = content;

            radio.Checked += (_, _) =>
            {
                if (radio.Tag is PasswordEntry e)
                {
                    _selectedEntry = e;
                    UpdateTotpPanel();
                }
            };

            EntriesPanel.Children.Add(radio);
            _entryButtons[entry] = radio;
            if (i == 0) _selectedEntry = entry;
        }
    }

    private void UpdateTotpPanel()
    {
        TotpBox.Items.Clear();
        _totpItems.Clear();
        TotpChoicePanel.Visibility = Visibility.Collapsed;
        TotpPreviewText.Text = string.Empty;
        _totpTimer.Stop();

        var host = HostOf(_selectedEntry?.Website ?? string.Empty);

        var matches = new List<TotpAccount>();
        foreach (var account in _allTotp)
        {
            if (string.IsNullOrEmpty(host))
            {
                matches.Add(account);
                continue;
            }

            var issuer = account.Issuer ?? string.Empty;
            var name = account.Name ?? string.Empty;
            if (issuer.IndexOf(host, StringComparison.OrdinalIgnoreCase) >= 0 ||
                host.IndexOf(issuer, StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf(host, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (_selectedEntry is not null && name.IndexOf(_selectedEntry.Title, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                matches.Add(account);
            }
        }

        // في وضع الكتابة إن لم يوجد تطابق نظهر كل الحسابات
        if (matches.Count == 0 && _typingMode)
            matches.AddRange(_allTotp);

        if (matches.Count == 0)
        {
            TotpPanel.Visibility = Visibility.Collapsed;
            return;
        }

        TotpPanel.Visibility = Visibility.Visible;
        FillTotpCheck.IsEnabled = true;

        for (var i = 0; i < matches.Count; i++)
        {
            var item = new ComboBoxItem { Content = matches[i].Name, Tag = matches[i] };
            TotpBox.Items.Add(item);
            _totpItems[matches[i]] = item;
            if (i == 0) TotpBox.SelectedItem = item;
        }

        FillTotpCheck.Checked += (_, _) =>
        {
            TotpChoicePanel.Visibility = Visibility.Visible;
            _totpTimer.Start();
            RefreshPreview();
        };
        FillTotpCheck.Unchecked += (_, _) =>
        {
            TotpChoicePanel.Visibility = Visibility.Collapsed;
            _totpTimer.Stop();
            TotpPreviewText.Text = string.Empty;
        };
        TotpBox.SelectionChanged += (_, _) => RefreshPreview();
    }

    private static string HostOf(string website)
    {
        if (string.IsNullOrWhiteSpace(website)) return string.Empty;
        if (Uri.TryCreate(website, UriKind.Absolute, out var uri))
            return uri.Host.ToLowerInvariant().TrimStart("www.".ToCharArray());
        return website.ToLowerInvariant().TrimStart("www.".ToCharArray());
    }

    private void RefreshPreview()
    {
        if (TotpBox.SelectedItem is ComboBoxItem { Tag: TotpAccount account })
        {
            var now = DateTimeOffset.Now;
            var code = TotpService.ComputeCode(account, now);
            var remaining = TotpService.SecondsRemaining(account, now);
            TotpPreviewText.Text = string.Format(Localization.Get("Bridge_CodePreview"), code, remaining);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_typingMode || _entries.Count == 0) return;
        var query = SearchBox.Text?.Trim() ?? string.Empty;
        var filtered = _entries
            .Where(x => string.IsNullOrEmpty(query)
                || x.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || x.Username.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || x.Website.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        EntriesPanel.Children.Clear();
        _entryButtons.Clear();
        _selectedEntry = null;

        if (filtered.Count == 0)
        {
            NoMatchText.Text = Localization.Get("Pass_Empty");
            NoMatchText.Visibility = Visibility.Visible;
            FillButton.IsEnabled = false;
            return;
        }

        NoMatchText.Visibility = Visibility.Collapsed;
        FillButton.IsEnabled = true;
        for (var i = 0; i < filtered.Count; i++)
        {
            var entry = filtered[i];
            var radio = new RadioButton
            {
                GroupName = "entries",
                Tag = entry,
                IsChecked = i == 0,
                Margin = new Thickness(0, 4, 0, 4),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var content = new StackPanel();
            content.Children.Add(new TextBlock { Text = entry.Title, FontWeight = FontWeights.SemiBold, FontSize = 13 });
            content.Children.Add(new TextBlock
            {
                Text = $"{entry.Username}  ·  {entry.Website}",
                FontSize = 11,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Margin = new Thickness(22, 2, 0, 0)
            });
            radio.Content = content;

            radio.Checked += (_, _) =>
            {
                if (radio.Tag is PasswordEntry e)
                {
                    _selectedEntry = e;
                    UpdateTotpPanel();
                }
            };

            EntriesPanel.Children.Add(radio);
            _entryButtons[entry] = radio;
            if (i == 0) _selectedEntry = entry;
        }
        UpdateTotpPanel();
    }

    private void FillButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEntry is null) return;

        var decision = new FillDecision
        {
            Decision = "fill",
            EntryTitle = _selectedEntry.Title
        };

        if (FillUsernameCheck.IsChecked == true && !string.IsNullOrEmpty(_selectedEntry.Username))
            decision.Username = _selectedEntry.Username;

        if (FillPasswordCheck.IsChecked == true && !string.IsNullOrEmpty(_selectedEntry.Password))
            decision.Password = _selectedEntry.Password;

        if (FillTotpCheck.IsChecked == true &&
            TotpBox.SelectedItem is ComboBoxItem { Tag: TotpAccount account })
        {
            decision.Totp = TotpService.ComputeCode(account, DateTimeOffset.Now);
            decision.TotpAccountName = account.Name;
        }

        Result = decision;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = new FillDecision { Decision = "none" };
        DialogResult = false;
    }
}
