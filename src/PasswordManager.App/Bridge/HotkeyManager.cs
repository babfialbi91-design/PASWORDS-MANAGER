using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace PasswordManager.App.Bridge;

/// <summary>
/// تسجيل الاختصار العام (RegisterHotKey) ومعالج WM_HOTKEY،
/// بالإضافة إلى التقاط أي مجموعة مفاتيح عبر خطاف لوحة مفاتيح منخفض المستوى.
/// </summary>
internal sealed class HotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int WmKeydown = 0x0100;
    private const int WmKeyup = 0x0101;

    private const uint WmHotkeyModifiers = 0x0000;

    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;

    private static readonly int[] ModifierVirtualKeys = [0x11 /*Ctrl*/, 0x10 /*Shift*/, 0x12 /*Alt*/, 0x5B /*Win*/];

    private static readonly Dictionary<ushort, string> VirtualKeyNames = new()
    {
        [0x08] = "Backspace", [0x09] = "Tab", [0x0D] = "Enter",
        [0x10] = "Shift", [0x11] = "Ctrl", [0x12] = "Alt", [0x5B] = "Win",
        [0x14] = "CapsLock", [0x20] = "Space", [0x25] = "Left", [0x26] = "Up",
        [0x27] = "Right", [0x28] = "Down", [0x2E] = "Delete", [0x2D] = "Insert",
        [0x21] = "PageUp", [0x22] = "PageDown", [0x24] = "Home", [0x23] = "End",
        [0x1B] = "Esc", [0x2C] = "PrintScreen", [0x2F] = "Help"
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern uint GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    private delegate nint HookProc(int nCode, nint wParam, nint lParam);

    private const int WhKeyboardLl = 13;

    private nint _hook = nint.Zero;
    private HookProc? _hookProc;
    private bool _capturing;
    private event Action<string>? Captured;

    private HwndSource? _source;
    private int _hotkeyId;
    private bool _registered;
    private Action? _pressed;

    /// <summary>بدء معالجة WM_HOTKEY للنافذة المحددة.</summary>
    public void Attach(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
    }

    public void SetPressed(Action? handler) => _pressed = handler;

    /// <summary>
    /// تسجيل اختصار. modifiers مثل "Control,Shift" والمفتاح اسم أو حرف.
    /// يعيد true عند النجاح.
    /// </summary>
    public bool Register(string modifiers, string key)
    {
        Unregister();

        var fs = ParseModifiers(modifiers);
        var vk = ParseKey(key);
        if (vk == 0) return false;

        var hwnd = _source?.Handle ?? nint.Zero;
        if (!RegisterHotKey(hwnd, 1, fs | ModNoRepeat, vk))
        {
            App.Log("Hotkey", new InvalidOperationException($"RegisterHotKey failed for key='{key}' vk=0x{vk:X} fs=0x{fs:X} on hwnd=0x{hwnd:X}, Win32 error {Marshal.GetLastWin32Error()}."));
            return false;
        }

        _hotkeyId = 1;
        _registered = true;
        return true;
    }

    public void Unregister()
    {
        if (!_registered) return;
        var hwnd = _source?.Handle ?? nint.Zero;
        UnregisterHotKey(hwnd, _hotkeyId);
        _registered = false;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmHotkey && (int)wParam == _hotkeyId)
        {
            handled = true;
            _pressed?.Invoke();
            return nint.Zero;
        }
        return nint.Zero;
    }

    private static uint ParseModifiers(string modifiers)
    {
        uint fs = 0;
        foreach (var part in modifiers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "control": case "ctrl": fs |= ModControl; break;
                case "shift": fs |= ModShift; break;
                case "alt": fs |= ModAlt; break;
                case "win": case "windows": fs |= ModWin; break;
            }
        }
        return fs;
    }

    private static uint ParseKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;
        if (key.Length == 1)
        {
            // ربط مباشر مستقل عن تخطيط لوحة المفاتيح: رموز VK للأحرف والأرقام
            // تساوي قيمة ASCII الكبيرة — VkKeyScan يفشل تحت التخطيط العربي.
            var c = char.ToUpperInvariant(key[0]);
            if (c is >= 'A' and <= 'Z') return c;
            if (c is >= '0' and <= '9') return c;
            return 0;
        }
        return key.ToLowerInvariant() switch
        {
            "space" => 0x20, "enter" => 0x0D, "tab" => 0x09, "esc" => 0x1B,
            "backspace" => 0x08, "delete" => 0x2E, "insert" => 0x2D,
            "home" => 0x24, "end" => 0x23, "pageup" => 0x21, "pagedown" => 0x22,
            "left" => 0x25, "up" => 0x26, "right" => 0x27, "down" => 0x28,
            "f1" => 0x70, "f2" => 0x71, "f3" => 0x72, "f4" => 0x73, "f5" => 0x74,
            "f6" => 0x75, "f7" => 0x76, "f8" => 0x77, "f9" => 0x78, "f10" => 0x79,
            "f11" => 0x7A, "f12" => 0x7B,
            _ => 0
        };
    }

    /// <summary>تنسيق مفتاح معرّف بصيغة نصية قابلة للعرض.</summary>
    public static string Describe(string modifiers, string key)
    {
        var parts = new List<string>();
        foreach (var part in modifiers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            parts.Add(part.ToLowerInvariant() switch
            {
                "control" or "ctrl" => "Ctrl",
                "shift" => "Shift",
                "alt" => "Alt",
                "win" or "windows" => "Win",
                _ => part
            });
        }

        var vk = ParseKey(key);
        var keyName = vk switch
        {
            0x41 => "A", 0x42 => "B", 0x43 => "C", 0x44 => "D", 0x45 => "E",
            0x46 => "F", 0x47 => "G", 0x48 => "H", 0x49 => "I", 0x4A => "J",
            0x4B => "K", 0x4C => "L", 0x4D => "M", 0x4E => "N", 0x4F => "O",
            0x50 => "P", 0x51 => "Q", 0x52 => "R", 0x53 => "S", 0x54 => "T",
            0x55 => "U", 0x56 => "V", 0x57 => "W", 0x58 => "X", 0x59 => "Y",
            0x5A => "Z",
            >= 0x30 and <= 0x39 => ((char)vk).ToString(),
            >= 0x70 and <= 0x7B => $"F{vk - 0x6F}",
            _ => VirtualKeyNames.TryGetValue((ushort)vk, out var name) ? name : key
        };

        parts.Add(keyName);
        return string.Join(" + ", parts);
    }

    // ─────────────────── التقاط اختصار ───────────────────

    /// <summary>
    /// بدء التقاط مجموعة المفاتيح التالية (يستدعي callback مع وصف المجموعة مثل "Ctrl+Shift+L").
    /// </summary>
    public void Capture(Action<string>? onCaptured)
    {
        if (onCaptured is null) return;
        _capturing = true;
        Captured = onCaptured;

        _hookProc ??= HookCallback;
        _hook = SetWindowsHookEx(WhKeyboardLl, _hookProc, nint.Zero, 0);
        if (_hook == nint.Zero)
        {
            _capturing = false;
            onCaptured(string.Empty);
        }
    }

    public void CancelCapture()
    {
        _capturing = false;
        Captured = null;
        ReleaseHook();
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && _capturing)
        {
            var msg = (int)wParam;
            var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            if (msg == WmKeydown)
            {
                var isModifier = Array.IndexOf(ModifierVirtualKeys, (int)kbd.vkCode) >= 0;
                if (!isModifier)
                {
                    var mainKey = DescribeKey(kbd.vkCode);

                    var modifiers = new List<string>();
                    if ((GetAsyncKeyState(0x11) & 0x8000) != 0) modifiers.Add("Ctrl");
                    if ((GetAsyncKeyState(0x10) & 0x8000) != 0) modifiers.Add("Shift");
                    if ((GetAsyncKeyState(0x12) & 0x8000) != 0) modifiers.Add("Alt");
                    if ((GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0) modifiers.Add("Win");

                    if (modifiers.Count > 0)
                    {
                        _capturing = false;
                        var handler = Captured;
                        Captured = null;
                        ReleaseHook();
                        var result = string.Join("+", modifiers) + "+" + mainKey;
                        try
                        {
                            Application.Current?.Dispatcher.Invoke(() => handler?.Invoke(result));
                        }
                        catch
                        {
                            handler?.Invoke(result);
                        }
                        return new nint(1); // منع المفتاح من الوصول للتطبيقات أثناء الالتقاط
                    }
                }
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private static string DescribeKey(uint vk)
    {
        if (VirtualKeyNames.TryGetValue((ushort)vk, out var name)) return name;
        if (vk >= 'A' && vk <= 'Z') return ((char)vk).ToString();
        if (vk >= '0' && vk <= '9') return ((char)vk).ToString();
        if (vk >= 0x70 && vk <= 0x7B) return $"F{vk - 0x6F}";
        var c = (char)VkKeyScan((char)vk);
        return c != '\0' ? c.ToString() : $"VK{vk:X}";
    }

    private void ReleaseHook()
    {
        if (_hook != nint.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = nint.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public nint dwExtraInfo;
    }

    public void Dispose()
    {
        CancelCapture();
        Unregister();
    }
}
