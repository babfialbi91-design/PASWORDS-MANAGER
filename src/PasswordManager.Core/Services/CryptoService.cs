using System.Security.Cryptography;

namespace PasswordManager.Services;

/// <summary>
/// خدمات التشفير: اشتقاق مفتاح من كلمة المرور الرئيسية (PBKDF2)
/// ثم تشفير الخزنة بـ AES-256-GCM (تشفير + مصادقة في خطوة واحدة).
/// </summary>
public static class CryptoService
{
    public const int KeySize = 32;                 // 256 bits
    public const int SaltSize = 16;                // 128 bits
    public const int NonceSize = 12;               // GCM nonce
    public const int TagSize = 16;                 // GCM tag
    public const int Iterations = 600_000;         // OWASP recommendation for PBKDF2-SHA256

    public static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltSize);

    public static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize);
    }

    public static (byte[] Nonce, byte[] Tag, byte[] Ciphertext) Encrypt(byte[] key, byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return (nonce, tag, ciphertext);
    }

    /// <summary>يفك التشفير، ويرمي CryptographicException إذا كانت كلمة المرور خاطئة أو تعبث بالملف.</summary>
    public static byte[] Decrypt(byte[] key, byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}
