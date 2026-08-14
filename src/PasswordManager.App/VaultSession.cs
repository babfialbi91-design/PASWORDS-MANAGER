using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.App;

/// <summary>جلسة الخزنة المفتوحة — يشاركها كل أجزاء الواجهة.</summary>
public sealed class VaultSession
{
    private readonly VaultService _vault;

    public VaultSession(VaultService vault, string masterPassword, VaultData data)
    {
        _vault = vault;
        MasterPassword = masterPassword;
        Data = data;
    }

    public VaultService Vault => _vault;

    public VaultData Data { get; }

    public string MasterPassword { get; set; }

    public string VaultPath => _vault.VaultPath;

    public event Action? Changed;

    public async Task SaveAsync()
    {
        await _vault.SaveAsync(MasterPassword, Data);
        Changed?.Invoke();
    }
}
