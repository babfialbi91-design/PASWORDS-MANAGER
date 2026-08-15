using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.App.Views;

public partial class LoginView : UserControl
{
    public VaultService? Vault { get; set; }

    private bool _isSetupMode;
    public bool IsSetupMode
    {
        get => _isSetupMode;
        set
        {
            if (_isSetupMode == value) return;
            _isSetupMode = value;
            ApplyMode();
        }
    }

    public string MasterPassword { get; private set; } = string.Empty;

    public VaultData Data { get; private set; } = new();

    public event EventHandler? Unlocked;

    private int _failedAttempts;
    private DateTime _blockUntil = DateTime.MinValue;

    public LoginView()
    {
        InitializeComponent();

        LangBox.Items.Add("العربية");
        LangBox.Items.Add("English");
        LangBox.SelectedIndex = Localization.Instance.IsRtl ? 0 : 1;

        Localization.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        LangBox.SelectedIndex = Localization.Instance.IsRtl ? 0 : 1;
        ApplyMode();
        UpdateStrength(CurrentPassword());
    }

    private void LangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LangBox.SelectedIndex < 0) return;
        Localization.Instance.Language = LangBox.SelectedIndex == 0 ? PasswordManager.App.Language.Arabic : PasswordManager.App.Language.English;
    }

    public void FocusFirst()
    {
        PasswordInput.Focus();
    }

    public void ResetForUnlock()
    {
        IsSetupMode = false;
        PasswordInput.Clear();
        PasswordText.Clear();
        ConfirmInput.Clear();
        ErrorText.Visibility = Visibility.Collapsed;
        StrengthText.Text = string.Empty;
        ApplyMode();
        FocusFirst();
    }

    public void ResetForSetup()
    {
        IsSetupMode = true;
        PasswordInput.Clear();
        PasswordText.Clear();
        ConfirmInput.Clear();
        SetError(string.Empty);
        StrengthText.Text = string.Empty;
        ApplyMode();
        FocusFirst();
    }

    private void ApplyMode()
    {
        if (IsSetupMode)
        {
            TitleText.Text = Localization.Get("Login_TitleSetup");
            SubtitleText.Text = Localization.Get("Login_SubSetup");
            ActionButton.Content = Localization.Get("Login_ActionSetup");
            ConfirmPanel.Visibility = Visibility.Visible;
            ResetLink.Visibility = Visibility.Collapsed;
        }
        else
        {
            TitleText.Text = Localization.Get("Login_TitleUnlock");
            SubtitleText.Text = Localization.Get("Login_SubUnlock");
            ActionButton.Content = Localization.Get("Login_ActionUnlock");
            ConfirmPanel.Visibility = Visibility.Collapsed;
            ResetLink.Visibility = Visibility.Visible;
        }
    }

    private void ShowPassword_Changed(object sender, RoutedEventArgs e)
    {
        var show = ShowPassword.IsChecked == true;
        PasswordInput.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        PasswordText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show)
            PasswordText.Text = PasswordInput.Password;
        else
            PasswordInput.Password = PasswordText.Text;
    }

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        UpdateStrength(PasswordInput.Password);
    }

    private void PasswordText_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateStrength(PasswordText.Text);
    }

    private void UpdateStrength(string password)
    {
        if (!IsSetupMode || string.IsNullOrEmpty(password))
        {
            StrengthText.Text = string.Empty;
            return;
        }

        var strength = PasswordQuality.Strength(password);
        var color = StrengthColor(strength);
        StrengthText.Text = string.Format(Localization.Get("Login_Strength"), Localization.Strength(strength));
        StrengthText.Foreground = color;
    }

    private static Brush StrengthColor(PasswordStrength strength)
        => strength switch
        {
            PasswordStrength.VeryStrong or PasswordStrength.Strong => (Brush)Application.Current.FindResource("SuccessBrush"),
            PasswordStrength.Medium => (Brush)Application.Current.FindResource("WarningBrush"),
            _ => (Brush)Application.Current.FindResource("DangerBrush")
        };

    private void ConfirmInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!IsSetupMode || string.IsNullOrEmpty(ConfirmInput.Password))
        {
            ConfirmHintText.Visibility = Visibility.Collapsed;
            return;
        }

        var match = ConfirmInput.Password == CurrentPassword();
        ConfirmHintText.Visibility = Visibility.Visible;
        if (match)
        {
            ConfirmHintText.Text = Localization.Get("Login_Match");
            ConfirmHintText.Foreground = (Brush)FindResource("SuccessBrush");
        }
        else
        {
            ConfirmHintText.Text = Localization.Get("Login_NoMatch");
            ConfirmHintText.Foreground = (Brush)FindResource("DangerBrush");
        }
    }

    private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter) return;
        if (IsSetupMode && sender == PasswordInput && CurrentPassword().Length >= 8)
        {
            ConfirmInput.Focus();
            e.Handled = true;
            return;
        }
        ActionButton_Click(sender, e);
        e.Handled = true;
    }

    private string CurrentPassword() =>
        ShowPassword.IsChecked == true ? PasswordText.Text : PasswordInput.Password;

    private async void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (Vault is null) return;

        if (DateTime.UtcNow < _blockUntil)
        {
            SetError(string.Format(Localization.Get("Login_ErrTooMany"),
                (int)Math.Ceiling((_blockUntil - DateTime.UtcNow).TotalSeconds)));
            return;
        }

        SetError(string.Empty);
        var password = CurrentPassword();

        try
        {
            if (IsSetupMode)
            {
                if (password.Length < 8)
                {
                    SetError(Localization.Get("Login_ErrTooShort"));
                    PasswordInput.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(ConfirmInput.Password))
                {
                    SetError(Localization.Get("Login_ErrNeedConfirm"));
                    ConfirmInput.Focus();
                    return;
                }
                if (password != ConfirmInput.Password)
                {
                    SetError(Localization.Get("Login_ErrMismatch"));
                    ConfirmInput.SelectAll();
                    ConfirmInput.Focus();
                    return;
                }

                Data = new VaultData();
                await Vault.CreateAsync(password, Data);
                MasterPassword = password;
            }
            else
            {
                Data = await Vault.OpenAsync(password);
                MasterPassword = password;
            }

            _failedAttempts = 0;
            _blockUntil = DateTime.MinValue;
            Unlocked?.Invoke(this, EventArgs.Empty);
        }
        catch (CryptographicException)
        {
            _failedAttempts++;
            if (_failedAttempts >= 5)
            {
                _failedAttempts = 0;
                _blockUntil = DateTime.UtcNow.AddSeconds(30);
                SetError(string.Format(Localization.Get("Login_ErrTooMany"), 30));
            }
            else
            {
                SetError(Localization.Get("Login_ErrWrongPassword"));
            }
        }
        catch (Exception ex)
        {
            SetError(string.Format(Localization.Get("Login_ErrOpenFailed"), ex.Message));
        }
    }

    private async void ResetLink_Click(object sender, RoutedEventArgs e)
    {
        if (Vault is null) return;

        var result = MessageBox.Show(
            Localization.Get("Login_ResetConfirmMsg"),
            Localization.Get("Login_ResetConfirmTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        await Task.Run(() =>
        {
            try { Vault.Reset(); }
            catch { /* قد لا يوجد ملف */ }
        });

        IsSetupMode = true;
        PasswordInput.Clear();
        PasswordText.Clear();
        ConfirmInput.Clear();
        SetError(string.Empty);
        ApplyMode();
        FocusFirst();
    }

    private void SetError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }
}
