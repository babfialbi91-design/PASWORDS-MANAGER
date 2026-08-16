using PasswordManager.App.Bridge;

namespace PasswordManager.App;

/// <summary>
/// متصفح مرتبط بالتطبيق عبر الجسر.
/// </summary>
public sealed class LinkedBrowser
{
    /// <summary>معرّف المتصفح في الكتالوج.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>مسار ملف التنفيذ المكتشف.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>نوع الربط المستخدم.</summary>
    public BridgeMethod Method { get; set; } = BridgeMethod.Typing;

    public BrowserInfo? Info => BrowserCatalog.Find(Id);
}
