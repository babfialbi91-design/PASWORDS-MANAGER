using System.Windows;
using System.Windows.Media;
using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.App.Dialogs;

public partial class EntryDialog : Window
{
    private readonly VaultSession _session;

    public PasswordEntry Entry { get; }

    public EntryDialog(VaultSession session, PasswordEntry? existing = null)
    {
        InitializeComponent();
        FlowDirection = Localization.Instance.IsRtl ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
        _session = session;

        if (existing is null)
        {
            Entry = new PasswordEntry();
            Title = Localization.Get("Entry_TitleAdd");
            WindowTitle.Text = "➕ " + Localization.Get("Entry_TitleAdd");
        }
        else
        {
            Entry = existing;
            Title = Localization.Get("Entry_TitleEdit");
            WindowTitle.Text = "✏️ " + Localization.Get("Entry_TitleEdit");
        }

        TitleInput.Text = Entry.Title;
        UsernameInput.Text = Entry.Username;
        WebsiteInput.Text = Entry.Website;
        CategoryInput.Text = string.IsNullOrEmpty(Entry.Category) ? "عام" : Entry.Category;
        PasswordInput.Text = Entry.Password;
        NotesInput.Text = Entry.Notes;

        UpdateStrength();
        Loaded += (_, _) => TitleInput.Focus();
    }

    public void PrefillGenerated(string password)
    {
        PasswordInput.Text = password;
        UpdateStrength();
    }

    private void UpdateStrength()
    {
        var strength = PasswordQuality.Strength(PasswordInput.Text);
        var color = strength switch
        {
            PasswordStrength.VeryStrong or PasswordStrength.Strong => (Brush)FindResource("SuccessBrush"),
            PasswordStrength.Medium => (Brush)FindResource("WarningBrush"),
            _ => (Brush)FindResource("DangerBrush")
        };
        StrengthText.Text = string.Format(Localization.Get("Entry_Strength"),
            Localization.Strength(strength), PasswordInput.Text.Length);
        StrengthText.Foreground = color;
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        var password = PasswordGenerator.Generate(new GeneratorOptions
        {
            Length = 18,
            UseLower = true,
            UseUpper = true,
            UseDigits = true,
            UseSymbols = true,
            ExcludeAmbiguous = true
        });
        PasswordInput.Text = password;
        UpdateStrength();
    }

    private void CopyGeneratedButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(PasswordInput.Text)) return;
        try
        {
            Clipboard.SetText(PasswordInput.Text);
            CopyGeneratedButton.Content = Localization.Get("Common_Copied");
        }
        catch
        {
            // تجاهل فشل النسخ
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleInput.Text))
        {
            MessageBox.Show(this, Localization.Get("Entry_ErrTitleRequired"), Localization.Get("Common_Notice"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TitleInput.Focus();
            return;
        }

        if (string.IsNullOrEmpty(PasswordInput.Text))
        {
            MessageBox.Show(this, Localization.Get("Entry_ErrPasswordRequired"), Localization.Get("Common_Notice"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            PasswordInput.Focus();
            return;
        }

        Entry.Title = TitleInput.Text.Trim();
        Entry.Username = UsernameInput.Text.Trim();
        Entry.Website = WebsiteInput.Text.Trim();
        Entry.Category = string.IsNullOrWhiteSpace(CategoryInput.Text) ? "عام" : CategoryInput.Text.Trim();
        Entry.Password = PasswordInput.Text;
        Entry.Notes = NotesInput.Text.Trim();
        Entry.UpdatedAt = DateTime.UtcNow;

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
