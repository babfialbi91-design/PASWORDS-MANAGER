using System.Runtime.InteropServices;

namespace PasswordManager.App.Bridge;

/// <summary>
/// كتابة نص في النافذة النشطة عبر SendInput بأحرف Unicode (تعمل مع كل التطبيقات والحقول).
/// </summary>
internal static class InputSimulator
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventUnicode = 0x0004;
    private const uint KeyEventKeyUp = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>ضغط مفتاح واحد (مثال: Tab للانتقال إلى الحقل التالي).</summary>
    public static void PressKey(ushort virtualKey)
    {
        var inputs = new INPUT[2];
        inputs[0] = new INPUT
        {
            type = InputKeyboard,
            U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey, wScan = 0, dwFlags = 0, dwExtraInfo = nint.Zero } }
        };
        inputs[1] = new INPUT
        {
            type = InputKeyboard,
            U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey, wScan = 0, dwFlags = KeyEventKeyUp, dwExtraInfo = nint.Zero } }
        };
        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    public static void TypeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var inputs = new INPUT[text.Length * 2];
        var index = 0;

        foreach (var c in text)
        {
            var scan = (ushort)c;

            inputs[index++] = new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scan,
                        dwFlags = KeyEventUnicode,
                        dwExtraInfo = nint.Zero
                    }
                }
            };

            inputs[index++] = new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scan,
                        dwFlags = KeyEventUnicode | KeyEventKeyUp,
                        dwExtraInfo = nint.Zero
                    }
                }
            };
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
