using System.Windows;
using System.Windows.Threading;

namespace PasswordManager.App;

/// <summary>
/// نسخ آمن للحافظة: يمسح المحتوى الحساس تلقائياً بعد فترة قصيرة، وعند قفل الخزنة أو الخروج.
/// </summary>
public static class SecureClipboard
{
    private const int DefaultClearSeconds = 30;

    private static readonly DispatcherTimer Timer = CreateTimer();
    private static string _sensitiveText = string.Empty;

    private static DispatcherTimer CreateTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(DefaultClearSeconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            TryClear();
        };
        return timer;
    }

    public static bool SetText(string text)
    {
        try
        {
            _sensitiveText = text;
            Clipboard.SetDataObject(text, true);
            Timer.Stop();
            Timer.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Clear()
    {
        Timer.Stop();
        TryClear();
    }

    private static void TryClear()
    {
        try
        {
            if (Clipboard.ContainsText() &&
                string.Equals(Clipboard.GetText(), _sensitiveText, StringComparison.Ordinal))
                Clipboard.Clear();
        }
        catch
        {
            // الحافظة قد تكون مشغولة من تطبيق آخر — تجاهل.
        }
        finally
        {
            _sensitiveText = string.Empty;
        }
    }
}
