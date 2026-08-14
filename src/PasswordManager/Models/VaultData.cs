namespace PasswordManager.Models;

/// <summary>الخزنة في الذاكرة بعد فتحها (بيانات غير مشفرة داخل الجلسة).</summary>
public sealed class VaultData
{
    public List<PasswordEntry> Passwords { get; set; } = new();
    public List<TotpAccount> TotpAccounts { get; set; } = new();
}

/// <summary>شكل الملف المشفر على القرص.</summary>
public sealed class VaultFile
{
    public int Version { get; set; } = 1;
    public int Iterations { get; set; }
    public string Salt { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}
