using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.App.Views;

public partial class GeneratorView : UserControl
{
    private VaultSession? _session;
    private string _current = string.Empty;
    private bool _ready;

    public GeneratorView()
    {
        InitializeComponent();
        Localization.LanguageChanged += () => { if (_ready) Generate(); };
        Loaded += (_, _) =>
        {
            _ready = true;
            Generate();
        };
    }

    public void Attach(VaultSession session)
    {
        _session = session;
    }

    public void Detach()
    {
        _session = null;
    }

    public void Refresh()
    {
        // لا حاجة لتحديث دوري — المحتوى يُتولد عند تغيير الخيارات.
    }

    private GeneratorOptions BuildOptions()
    {
        return new GeneratorOptions
        {
            Length = (int)LengthSlider.Value,
            UseUpper = UseUpper.IsChecked == true,
            UseLower = UseLower.IsChecked == true,
            UseDigits = UseDigits.IsChecked == true,
            UseSymbols = UseSymbols.IsChecked == true,
            ExcludeAmbiguous = ExcludeAmbiguous.IsChecked == true
        };
    }

    private void Options_Changed(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        Generate();
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e) => Generate();

    private void Generate()
    {
        LengthValue.Text = ((int)LengthSlider.Value).ToString();

        try
        {
            var options = BuildOptions();
            _current = PasswordGenerator.Generate(options);
            ResultText.Text = _current;
            NoticeText.Text = string.Empty;

            var strength = PasswordQuality.Strength(_current);
            StrengthText.Text = Localization.Strength(strength);
            StrengthText.Foreground = strength switch
            {
                PasswordStrength.VeryStrong or PasswordStrength.Strong => (Brush)FindResource("SuccessBrush"),
                PasswordStrength.Medium => (Brush)FindResource("WarningBrush"),
                _ => (Brush)FindResource("DangerBrush")
            };

            var entropy = PasswordGenerator.EstimateBitsOfEntropy(options);
            EntropyText.Text = string.Format(Localization.Get("Gen_Bits"), entropy);
            EntropyText.Foreground = (Brush)FindResource("TextBrush");
        }
        catch (Exception ex)
        {
            _current = string.Empty;
            ResultText.Text = "—";
            StrengthText.Text = "—";
            EntropyText.Text = "—";
            NoticeText.Text = $"⚠ {ex.Message}";
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_current)) return;

        try
        {
            Clipboard.SetText(_current);
            CopyButton.Content = Localization.Get("Gen_Copied");
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (_, _) => { CopyButton.Content = Localization.Get("Gen_Copy"); timer.Stop(); };
            timer.Start();
        }
        catch
        {
            MessageBox.Show(Localization.Get("Common_ClipboardFailed"), Localization.Get("Common_Error"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null) return;
        if (string.IsNullOrEmpty(_current)) return;

        var dialog = new Dialogs.EntryDialog(_session)
        {
            Owner = Window.GetWindow(this)
        };
        dialog.PrefillGenerated(_current);

        if (dialog.ShowDialog() == true)
        {
            _session.Data.Passwords.Add(dialog.Entry);
            await _session.SaveAsync();
            NoticeText.Text = string.Format(Localization.Get("Gen_Saved"), dialog.Entry.Title);
            NoticeText.Foreground = (Brush)FindResource("SuccessBrush");
        }
    }
}
