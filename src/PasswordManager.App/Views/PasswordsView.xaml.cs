using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.App.Views;

public partial class PasswordsView : UserControl
{
    private VaultSession? _session;
    private bool _revealed;

    public PasswordsView()
    {
        InitializeComponent();
    }

    public void Attach(VaultSession session)
    {
        _session = session;
        _session.Changed += Refresh;
        Refresh();
    }

    public void Detach()
    {
        if (_session is not null)
            _session.Changed -= Refresh;
        _session = null;
    }

    public void Refresh()
    {
        if (_session is null) return;

        var entries = _session.Data.Passwords.OrderByDescending(e => e.UpdatedAt).ToList();
        var term = SearchBox.Text.Trim();
        if (term.Length > 0)
        {
            entries = entries.Where(e =>
                e.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Username.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Website.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var selected = EntriesList.SelectedItem as PasswordEntry;
        EntriesList.ItemsSource = entries;

        if (selected is not null)
            EntriesList.SelectedItem = entries.FirstOrDefault(e => e.Id == selected.Id);

        CountText.Text = $"المحفوظ: {_session.Data.Passwords.Count} | المعروض: {entries.Count}";
        EmptyText.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (EntriesList.SelectedItem is null)
            DetailsPanel.Visibility = Visibility.Collapsed;
        else
            ShowDetails();
    }

    private void ShowDetails(PasswordEntry? entry = null)
    {
        entry ??= EntriesList.SelectedItem as PasswordEntry;
        if (entry is null) return;

        DetailsTitle.Text = entry.Title;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(entry.Username)) parts.Add($"المستخدم: {entry.Username}");
        if (!string.IsNullOrEmpty(entry.Website)) parts.Add($"الموقع: {entry.Website}");
        if (!string.IsNullOrEmpty(entry.Category)) parts.Add($"التصنيف: {entry.Category}");
        if (!string.IsNullOrEmpty(entry.Notes)) parts.Add($"ملاحظات: {entry.Notes}");
        parts.Add($"القوة: {PasswordQuality.StrengthLabel(entry.Password)}");
        DetailsMeta.Text = string.Join("   •   ", parts);

        PasswordDisplay.Text = _revealed ? entry.Password : new string('•', Math.Clamp(entry.Password.Length, 1, 24));
        ToggleRevealButton.Content = _revealed ? "🙈 إخفاء" : "👁️ إظهار";
        DetailsPanel.Visibility = Visibility.Visible;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void EntriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _revealed = false;
        ShowDetails();
    }

    private void EntriesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EntriesList.SelectedItem is PasswordEntry entry)
            _ = EditEntryAsync(entry);
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null) return;

        var dialog = new Dialogs.EntryDialog(_session)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            _session.Data.Passwords.Add(dialog.Entry);
            await _session.SaveAsync();
            EntriesList.SelectedItem = dialog.Entry;
            Refresh();
        }
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesList.SelectedItem is PasswordEntry entry)
            await EditEntryAsync(entry);
    }

    private async Task EditEntryAsync(PasswordEntry entry)
    {
        if (_session is null) return;

        var dialog = new Dialogs.EntryDialog(_session, entry)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            entry.UpdatedAt = DateTime.UtcNow;
            await _session.SaveAsync();
            Refresh();
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null || EntriesList.SelectedItem is not PasswordEntry entry) return;

        var result = MessageBox.Show(
            $"هل تريد حذف «{entry.Title}» نهائياً؟",
            "تأكيد الحذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _session.Data.Passwords.Remove(entry);
        await _session.SaveAsync();
        Refresh();
    }

    private void CopyPassword_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesList.SelectedItem is PasswordEntry entry)
            CopyText(entry.Password, "تم نسخ كلمة المرور إلى الحافظة.");
    }

    private void CopyUsername_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesList.SelectedItem is PasswordEntry entry)
            CopyText(entry.Username, "تم نسخ اسم المستخدم إلى الحافظة.");
    }

    private void ToggleReveal_Click(object sender, RoutedEventArgs e)
    {
        _revealed = !_revealed;
        ShowDetails();
    }

    private void CopyText(string text, string successMessage)
    {
        try
        {
            Clipboard.SetText(text);
            var win = Window.GetWindow(this) as MainWindow;
            win?.Notify(successMessage);
        }
        catch
        {
            MessageBox.Show("تعذّر النسخ إلى الحافظة.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
