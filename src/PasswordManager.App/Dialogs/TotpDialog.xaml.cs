using System.Windows;
using System.Windows.Controls;
using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.App.Dialogs;

public partial class TotpDialog : Window
{
    private bool _fromUri;

    public TotpAccount? Account { get; private set; }

    public TotpDialog()
    {
        InitializeComponent();
        FlowDirection = Localization.Instance.IsRtl ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
        Loaded += (_, _) => SecretInput.Focus();
    }

    private void SecretInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        var input = SecretInput.Text.Trim();

        if (input.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
        {
            var parsed = TotpService.ParseInput(input, string.Empty);
            if (parsed is not null)
            {
                _fromUri = true;
                NameInput.Text = parsed.Name;
                AlgorithmBox.IsEnabled = false;
                DigitsBox.IsEnabled = false;
                PeriodBox.IsEnabled = false;

                AlgorithmBox.SelectedItem = AlgorithmBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => (i.Content as string) == parsed.Algorithm) ?? AlgorithmBox.Items[0];
                DigitsBox.SelectedItem = DigitsBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => (i.Content as string) == parsed.Digits.ToString()) ?? DigitsBox.Items[0];
                PeriodBox.SelectedItem = PeriodBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => (i.Content as string) == parsed.Period.ToString()) ?? PeriodBox.Items[0];
            }
        }
        else if (_fromUri)
        {
            _fromUri = false;
            NameInput.Clear();
            AlgorithmBox.IsEnabled = true;
            DigitsBox.IsEnabled = true;
            PeriodBox.IsEnabled = true;
        }

        SetError(string.Empty);
        PreviewText.Text = string.Empty;
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        var account = TryBuild(out var message);
        if (account is null)
        {
            SetError(message);
            return;
        }

        try
        {
            PreviewText.Text = TotpService.ComputeCode(account, DateTimeOffset.Now);
            SetError(string.Empty);
        }
        catch (Exception ex)
        {
            SetError(string.Format(Localization.Get("TotpDialog_ErrKey"), ex.Message));
        }
    }

    private TotpAccount? TryBuild(out string message)
    {
        message = string.Empty;
        var input = SecretInput.Text.Trim();

        if (string.IsNullOrEmpty(input))
        {
            message = Localization.Get("TotpDialog_ErrSecret");
            return null;
        }

        var parsed = TotpService.ParseInput(input, NameInput.Text.Trim());

        if (parsed is null)
        {
            message = Localization.Get("TotpDialog_ErrInvalidInput");
            return null;
        }

        if (!_fromUri)
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                message = Localization.Get("TotpDialog_ErrName");
                return null;
            }

            parsed.Name = NameInput.Text.Trim();
            parsed.Algorithm = ((AlgorithmBox.SelectedItem as ComboBoxItem)?.Content as string) ?? "SHA1";
            parsed.Digits = int.Parse(((DigitsBox.SelectedItem as ComboBoxItem)?.Content as string) ?? "6");
            parsed.Period = int.Parse(((PeriodBox.SelectedItem as ComboBoxItem)?.Content as string) ?? "30");
        }

        return parsed;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var account = TryBuild(out var message);
        if (account is null)
        {
            SetError(message);
            return;
        }

        try
        {
            _ = TotpService.ComputeCode(account, DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            SetError(string.Format(Localization.Get("TotpDialog_ErrKey"), ex.Message));
            return;
        }

        Account = account;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void SetError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }
}
