using System.Text.Json;
using PasswordManager.Models;

namespace PasswordManager.Services;

/// <summary>
/// يدير دورة حياة الخزنة: إنشاء، فتح، حفظ، تغيير كلمة المرور، إعادة التعيين.
/// الملف يُحفظ مشفراً كلياً على القرص.
/// </summary>
public sealed class VaultService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _vaultPath;

    public VaultService(string vaultPath)
    {
        _vaultPath = vaultPath;
    }

    public bool Exists => File.Exists(_vaultPath);

    public string VaultPath => _vaultPath;

    // ------------------------------------------------------------------

    public async Task CreateAsync(string masterPassword, VaultData data)
    {
        var salt = CryptoService.GenerateSalt();
        var key = CryptoService.DeriveKey(masterPassword, salt, CryptoService.Iterations);
        var file = Seal(key, salt, CryptoService.Iterations, data);
        await WriteFileAsync(file);
    }

    public async Task<VaultData> OpenAsync(string masterPassword)
    {
        var file = await ReadFileAsync();
        var key = CryptoService.DeriveKey(masterPassword, Convert.FromBase64String(file.Salt), file.Iterations);
        var plaintext = CryptoService.Decrypt(
            key,
            Convert.FromBase64String(file.Nonce),
            Convert.FromBase64String(file.Tag),
            Convert.FromBase64String(file.Data));

        return JsonSerializer.Deserialize<VaultData>(plaintext, JsonOptions) ?? new VaultData();
    }

    public async Task SaveAsync(string masterPassword, VaultData data)
    {
        var file = await ReadFileAsync();
        var key = CryptoService.DeriveKey(masterPassword, Convert.FromBase64String(file.Salt), file.Iterations);
        var sealedFile = Seal(key, Convert.FromBase64String(file.Salt), file.Iterations, data);
        await WriteFileAsync(sealedFile);
    }

    public async Task ChangeMasterPasswordAsync(string oldPassword, string newPassword)
    {
        var data = await OpenAsync(oldPassword);
        await CreateAsync(newPassword, data);
    }

    public void Reset() => File.Delete(_vaultPath);

    // ------------------------------------------------------------------

    private VaultFile Seal(byte[] key, byte[] salt, int iterations, VaultData data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var (nonce, tag, ciphertext) = CryptoService.Encrypt(key, System.Text.Encoding.UTF8.GetBytes(json));

        return new VaultFile
        {
            Version = 1,
            Iterations = iterations,
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            Data = Convert.ToBase64String(ciphertext)
        };
    }

    private async Task<VaultFile> ReadFileAsync()
    {
        if (!Exists)
            throw new FileNotFoundException("ملف الخزنة غير موجود.", _vaultPath);

        await using var stream = File.OpenRead(_vaultPath);
        return await JsonSerializer.DeserializeAsync<VaultFile>(stream, JsonOptions) ?? throw new InvalidDataException("ملف الخزنة تالف.");
    }

    private async Task WriteFileAsync(VaultFile file)
    {
        var dir = Path.GetDirectoryName(_vaultPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var stream = File.Create(_vaultPath);
        await JsonSerializer.SerializeAsync(stream, file, JsonOptions);
    }
}
