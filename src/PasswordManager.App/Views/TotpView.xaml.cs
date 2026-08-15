using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PasswordManager.Services;

namespace PasswordManager.App.Views;

public partial class TotpView : UserControl
{
    private VaultSession? _session;
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<string, Card> _cards = new();

    private sealed class Card
    {
        public required TextBlock Code { get; init; }
        public required ProgressBar Bar { get; init; }
        public required TextBlock Remaining { get; init; }
    }

    public TotpView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => Tick();
        Localization.LanguageChanged += Refresh;
    }

    public void Attach(VaultSession session)
    {
        _session = session;
        _session.Changed += Refresh;
        _timer.Start();
        Refresh();
    }

    public void Detach()
    {
        _timer.Stop();
        if (_session is not null)
            _session.Changed -= Refresh;
        _session = null;
        _cards.Clear();
        AccountsPanel.Children.Clear();
    }

    public void Refresh()
    {
        if (_session is null) return;

        _cards.Clear();
        AccountsPanel.Children.Clear();

        var accounts = _session.Data.TotpAccounts.OrderBy(a => a.Name).ToList();
        EmptyText.Visibility = accounts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var account in accounts)
            AccountsPanel.Children.Add(BuildCard(account));

        Tick();
    }

    private Border BuildCard(Models.TotpAccount account)
    {
        var surface = (Brush)FindResource("SurfaceBrush");
        var border = (Brush)FindResource("BorderBrush");
        var accent = (Brush)FindResource("AccentBrush");
        var muted = (Brush)FindResource("TextMutedBrush");

        var card = new Border
        {
            Background = surface,
            CornerRadius = new CornerRadius(12),
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(20, 14, 20, 14)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameBlock = new TextBlock
        {
            Text = account.Name,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var codeBlock = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 30,
            FontWeight = FontWeights.Bold,
            Foreground = accent,
            Margin = new Thickness(0, 6, 0, 0)
        };
        var remainingBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = muted,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var rightPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        rightPanel.Children.Add(nameBlock);
        rightPanel.Children.Add(codeBlock);
        rightPanel.Children.Add(remainingBlock);
        grid.Children.Add(rightPanel);

        var leftPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Width = 240,
            Margin = new Thickness(20, 0, 0, 0)
        };
        Grid.SetColumn(leftPanel, 1);

        var progress = new ProgressBar
        {
            Height = 8,
            Maximum = account.Period,
            Value = account.Period
        };
        leftPanel.Children.Add(progress);

        var buttonsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };

        var copyButton = new Button
        {
            Content = Localization.Get("Common_Copy"),
            Style = (Style)FindResource("SecondaryButton"),
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 0)
        };
        copyButton.Click += (_, _) => CopyCode(account);

        var deleteButton = new Button
        {
            Content = Localization.Get("Common_Delete"),
            Style = (Style)FindResource("DangerButton"),
            Padding = new Thickness(12, 6, 12, 6)
        };
        deleteButton.Click += async (_, _) => await DeleteAccountAsync(account);

        buttonsRow.Children.Add(copyButton);
        buttonsRow.Children.Add(deleteButton);
        leftPanel.Children.Add(buttonsRow);
        grid.Children.Add(leftPanel);

        card.Child = grid;

        _cards[account.Id] = new Card { Code = codeBlock, Bar = progress, Remaining = remainingBlock };
        return card;
    }

    private void Tick()
    {
        if (_session is null) return;

        var danger = (Brush)FindResource("DangerBrush");
        var success = (Brush)FindResource("SuccessBrush");
        var muted = (Brush)FindResource("TextMutedBrush");

        foreach (var account in _session.Data.TotpAccounts)
        {
            if (!_cards.TryGetValue(account.Id, out var card)) continue;

            try
            {
                var now = DateTimeOffset.Now;
                var code = TotpService.ComputeCode(account, now);
                var remaining = TotpService.SecondsRemaining(account, now);

                card.Code.Text = code;
                card.Bar.Value = remaining;
                card.Remaining.Text = string.Format(Localization.Get("Totp_Remaining"), remaining);
                var urgent = remaining <= 5;
                card.Bar.Foreground = urgent ? danger : success;
                card.Remaining.Foreground = urgent ? danger : muted;
            }
            catch
            {
                card.Code.Text = Localization.Get("Totp_InvalidKey");
                card.Bar.Value = 0;
                card.Remaining.Text = string.Empty;
            }
        }
    }

    private void CopyCode(Models.TotpAccount account)
    {
        try
        {
            var code = TotpService.ComputeCode(account, DateTimeOffset.Now);
            Clipboard.SetText(code);
            var win = Window.GetWindow(this) as MainWindow;
            win?.Notify(string.Format(Localization.Get("Totp_Copied"), account.Name));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Localization.Get("Common_Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task DeleteAccountAsync(Models.TotpAccount account)
    {
        if (_session is null) return;

        var result = MessageBox.Show(
            string.Format(Localization.Get("Totp_DeleteConfirm"), account.Name),
            Localization.Get("Common_ConfirmDelete"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _session.Data.TotpAccounts.Remove(account);
        await _session.SaveAsync();
        Refresh();
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null) return;

        var dialog = new Dialogs.TotpDialog
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && dialog.Account is not null)
        {
            _session.Data.TotpAccounts.Add(dialog.Account);
            await _session.SaveAsync();
            Refresh();
        }
    }
}
