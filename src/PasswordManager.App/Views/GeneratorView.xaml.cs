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

    public GeneratorView()
    {
        InitializeComponent();
        Loaded += (_, _) => Generate();
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

    private void Options_Changed(object sender, RoutedEventArgs e) => Generate();

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

            var label = PasswordQuality.StrengthLabel(_current);
            var color = label switch
            {
                "قوية جداً" => (Brush)FindResource("SuccessBrush"),
                "قوية" => (Brush)FindResource("SuccessBrush"),
                "متوسطة" => (Brush)FindResource("WarningBrush"),
                _ => (Brush)FindResource("DangerBrush")
            };
            StrengthText.Text = label;
            StrengthText.Foreground = color;

            var entropy = PasswordGenerator.EstimateBitsOfEntropy(options);
            EntropyText.Text = $"{entropy:0} بت";
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
            CopyButton.Content = "✓  تم النسخ";
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (_, _) => { CopyButton.Content = "📋  نسخ"; timer.Stop(); };
            timer.Start();
        }
        catch
        {
            MessageBox.Show("تعذّر النسخ إلى الحافظة.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            NoticeText.Text = $"✓ تم حفظ كلمة المرور للموقع «{dialog.Entry.Title}» في الخزنة.";
            NoticeText.Foreground = (Brush)FindResource("SuccessBrush");
        }
    }
}
