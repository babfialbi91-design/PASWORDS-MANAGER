using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using PasswordManager.App.Dialogs;
using PasswordManager.Models;

namespace PasswordManager.App.Bridge;

/// <summary>
/// جسر الكتابة: يفتح لوحة اختيار، وعند الاختيار يعيد التركيز إلى النافذة السابقة
/// (المتصفح) ويكتب القيم المختارة في الحقول النشطة.
/// </summary>
internal static class TypingBridge
{
    private const ushort VkTab = 0x09;
    private const ushort VkReturn = 0x0D;

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    /// <summary>
    /// فتح اللوحة وإجراء الكتابة. يستدعى من معالج الاختصار العام.
    /// </summary>
    public static void ShowAndType(VaultSession session, Window owner)
    {
        var previous = GetForegroundWindow();

        var window = new FillConfirmWindow
        {
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        window.Setup(
            typingMode: true,
            entries: session.Data.Passwords,
            allTotp: session.Data.TotpAccounts);

        var shown = window.ShowDialog() == true;
        if (!shown || window.Result?.Decision != "fill") return;

        var values = new List<string>();
        if (!string.IsNullOrEmpty(window.Result.Username)) values.Add(window.Result.Username);
        if (!string.IsNullOrEmpty(window.Result.Password)) values.Add(window.Result.Password);
        if (!string.IsNullOrEmpty(window.Result.Totp)) values.Add(window.Result.Totp);

        RestoreForeground(previous);
        TypeValues(values);
    }

    private static void TypeValues(IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            InputSimulator.TypeText(values[i]);
            if (i < values.Count - 1)
            {
                Thread.Sleep(40);
                InputSimulator.PressKey(VkTab);
            }
            Thread.Sleep(60);
        }
    }

    /// <summary>إعادة التركيز إلى نافذة المتصفح قبل الكتابة.</summary>
    public static void RestoreForeground(nint hWnd)
    {
        if (hWnd == nint.Zero) return;

        var currentThread = GetCurrentThreadId();
        var targetThread = GetWindowThreadProcessId(hWnd, out _);

        try
        {
            if (targetThread != 0 && targetThread != currentThread)
                AttachThreadInput(currentThread, targetThread, true);
            SetForegroundWindow(hWnd);
            if (targetThread != 0 && targetThread != currentThread)
                AttachThreadInput(currentThread, targetThread, false);
        }
        catch
        {
            // تجاهل
        }

        Thread.Sleep(120);
    }
}
