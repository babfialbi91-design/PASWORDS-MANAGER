using System.IO;

namespace PasswordManager.App.Bridge;

/// <summary>
/// ثوابت جسر المتصفحات: هوية الامتداد، أسماء الملفات، أسماء السجلات والممرات.
/// </summary>
internal static class BridgeConstants
{
    /// <summary>اسم المضيف الأصلي في Native Messaging.</summary>
    public const string HostName = "com.pasman.bridge";

    /// <summary>المعرّف الثابت للامتداد — مشتق من مفتاح RSA العام المخزن في الأصول.</summary>
    public const string ExtensionId = "nhfgjiijhlaaggbdiklehjidglgbpedp";

    /// <summary>اسم الممر المسماة بين عملية الجسر والتطبيق الرئيسي.</summary>
    public const string PipeName = "pasman_bridge";

    /// <summary>وسم المعامل في سطر الأوامر لوضع الجسر.</summary>
    public const string BridgeArg = "--pasman-bridge";

    /// <summary>وسم اسم المتصفح المرسل من الامتداد.</summary>
    public const string BrowserArgPrefix = "--pasman-browser=";

    public static string AppDataDir
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PasswordManager");

    public static string BridgeDir => Path.Combine(AppDataDir, "bridge");

    public static string HostManifestsDir => Path.Combine(BridgeDir, "host");

    public static string ExtensionDir => Path.Combine(BridgeDir, "extension");
}
