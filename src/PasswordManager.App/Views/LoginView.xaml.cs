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

    public LoginView()
    {
        InitializeComponent();
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
            TitleText.Text = "إنشاء خزنة جديدة";
            SubtitleText.Text = "هذه أول مرة تستخدم فيها الأداة — حدد كلمة مرور رئيسية تُشفّر بها كل بياناتك. إن نسيتها لن يستطيع أحد استرجاعها.";
            ActionButton.Content = "إنشاء الخزنة";
            ConfirmPanel.Visibility = Visibility.Visible;
            ResetLink.Visibility = Visibility.Collapsed;
        }
        else
        {
            TitleText.Text = "فتح الخزنة";
            SubtitleText.Text = "أدخل كلمة المرور الرئيسية لفتح بياناتك.";
            ActionButton.Content = "دخول";
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

        var label = PasswordQuality.StrengthLabel(password);
        var color = label switch
        {
            "قوية جداً" => (Brush)FindResource("SuccessBrush"),
            "قوية" => (Brush)FindResource("SuccessBrush"),
            "متوسطة" => (Brush)FindResource("WarningBrush"),
            _ => (Brush)FindResource("DangerBrush")
        };
        StrengthText.Text = $"القوة: {label}";
        StrengthText.Foreground = color;
    }

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
            ConfirmHintText.Text = "✓ متطابقة";
            ConfirmHintText.Foreground = (Brush)FindResource("SuccessBrush");
        }
        else
        {
            ConfirmHintText.Text = "غير متطابقة — راجع كلمة المرور";
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

        SetError(string.Empty);
        var password = CurrentPassword();

        try
        {
            if (IsSetupMode)
            {
                if (password.Length < 8)
                {
                    SetError("كلمة المرور يجب أن تكون 8 أحرف على الأقل.");
                    PasswordInput.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(ConfirmInput.Password))
                {
                    SetError("أعد كتابة نفس كلمة المرور في حقل التأكيد ثم تابع.");
                    ConfirmInput.Focus();
                    return;
                }
                if (password != ConfirmInput.Password)
                {
                    SetError("كلمتا المرور غير متطابقتين — تأكد أنهما متماثلتان.");
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

            Unlocked?.Invoke(this, EventArgs.Empty);
        }
        catch (CryptographicException)
        {
            SetError("كلمة المرور غير صحيحة.");
        }
        catch (Exception ex)
        {
            SetError($"تعذّر فتح الخزنة: {ex.Message}");
        }
    }

    private async void ResetLink_Click(object sender, RoutedEventArgs e)
    {
        if (Vault is null) return;

        var result = MessageBox.Show(
            "سيتم حذف كل كلمات المرور وحسابات TOTP نهائياً ولا يمكن استرجاعها.\nهل تريد المتابعة؟",
            "إعادة تعيين الخزنة",
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
