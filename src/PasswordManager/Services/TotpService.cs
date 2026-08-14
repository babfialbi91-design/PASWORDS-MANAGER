using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PasswordManager.Models;

namespace PasswordManager.Services;

/// <summary>
/// تنفيذ TOTP وفق RFC 6238 (المعيار المستخدم في تطبيقات المصادقة الثنائية مثل Google Authenticator).
/// يدعم SHA1 / SHA256 / SHA512، 6-8 خانات، وفترات زمنية مختلفة.
/// </summary>
public static class TotpService
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string[] SupportedAlgorithms => ["SHA1", "SHA256", "SHA512"];
    public static int[] SupportedDigits => [6, 7, 8];

    // ----------------------------------------------------------------

    public static string ComputeCode(string secretBase32, string algorithm, int digits, int period, DateTimeOffset now)
    {
        var key = DecodeBase32(secretBase32);
        var counter = now.ToUnixTimeSeconds() / period;
        return ComputeHmacCode(key, algorithm, digits, counter);
    }

    public static string ComputeCode(TotpAccount account, DateTimeOffset now)
        => ComputeCode(account.SecretBase32, account.Algorithm, account.Digits, account.Period, now);

    public static int SecondsRemaining(TotpAccount account, DateTimeOffset now)
    {
        var rem = account.Period - (int)(now.ToUnixTimeSeconds() % account.Period);
        return rem == 0 ? account.Period : rem;
    }

    public static bool IsValidSecret(string secret) => TryDecodeBase32(NormalizeSecret(secret), out _);

    // ----------------------------------------------------------------

    /// <summary>
    /// يحلل السطر المدخل: إما رابط otpauth:// أو المفتاح Base32 مباشرة.
    /// يرجع حساباً جاهزاً أو null إذا تعذّر التحليل.
    /// </summary>
    public static TotpAccount? ParseInput(string input, string accountName)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        if (input.StartsWith("otpauth://totp/", StringComparison.OrdinalIgnoreCase))
            return ParseOtpauthUri(input);

        var normalized = NormalizeSecret(input);
        if (!TryDecodeBase32(normalized, out var bytes) || bytes.Length == 0)
            return null;

        return new TotpAccount
        {
            Name = string.IsNullOrWhiteSpace(accountName) ? "حساب بدون اسم" : accountName.Trim(),
            SecretBase32 = normalized,
            Algorithm = "SHA1",
            Digits = 6,
            Period = 30
        };
    }

    private static TotpAccount? ParseOtpauthUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
            return null;

        var path = Uri.UnescapeDataString(parsed.AbsolutePath.TrimStart('/'));
        var labelParts = path.Split(':', 2);
        var name = labelParts.Length > 1
            ? $"{labelParts[0]} - {labelParts[1]}"
            : path;

        var query = ParseQueryString(parsed.Query);
        var secret = query.GetValueOrDefault("secret") ?? string.Empty;

        var normalized = NormalizeSecret(secret);
        if (!TryDecodeBase32(normalized, out _))
            return null;

        return new TotpAccount
        {
            Name = name,
            SecretBase32 = normalized,
            Algorithm = (query.GetValueOrDefault("algorithm") ?? "SHA1").ToUpperInvariant(),
            Digits = int.TryParse(query.GetValueOrDefault("digits"), out var d) ? Math.Clamp(d, 6, 8) : 6,
            Period = int.TryParse(query.GetValueOrDefault("period"), out var p) && p > 0 ? p : 30
        };
    }

    // ----------------------------------------------------------------

    private static string ComputeHmacCode(byte[] key, string algorithm, int digits, long counter)
    {
        var hash = algorithm.ToUpperInvariant() switch
        {
            "SHA256" => ComputeHmac(key, counter, HashAlgorithmName.SHA256),
            "SHA512" => ComputeHmac(key, counter, HashAlgorithmName.SHA512),
            _ => ComputeHmac(key, counter, HashAlgorithmName.SHA1)
        };

        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var mod = 1;
        for (var i = 0; i < digits; i++) mod *= 10;
        return (binary % mod).ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }

    private static byte[] ComputeHmac(byte[] key, long counter, HashAlgorithmName algorithm)
    {
        var counterBytes = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        return algorithm.Name switch
        {
            "SHA256" => HMAC_Hash(new HMACSHA256(key), counterBytes),
            "SHA512" => HMAC_Hash(new HMACSHA512(key), counterBytes),
            _ => HMAC_Hash(new HMACSHA1(key), counterBytes)
        };
    }

    private static byte[] HMAC_Hash(HMAC hmac, byte[] input)
    {
        using (hmac)
            return hmac.ComputeHash(input);
    }

    // ----------------------------------------------------------------

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
            return result;

        query = query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx < 0) continue;

            var key = Uri.UnescapeDataString(pair[..idx].Replace('+', ' '));
            var value = Uri.UnescapeDataString(pair[(idx + 1)..].Replace('+', ' '));
            if (!string.IsNullOrEmpty(key) && !result.ContainsKey(key))
                result[key] = value;
        }
        return result;
    }

    private static string NormalizeSecret(string secret)
    {
        var sb = new StringBuilder(secret.Length);
        foreach (var c in secret)
        {
            if (char.IsWhiteSpace(c) || c == '-' || c == '_')
                continue;
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    private static bool TryDecodeBase32(string input, out byte[] result)
    {
        result = Array.Empty<byte>();
        if (string.IsNullOrEmpty(input))
            return false;

        // إزالة الحشو =
        input = input.TrimEnd('=');

        var bits = new List<byte>(input.Length * 5);
        foreach (var c in input)
        {
            var idx = Base32Alphabet.IndexOf(c);
            if (idx < 0)
                return false;
            for (var b = 4; b >= 0; b--)
                bits.Add((byte)((idx >> b) & 1));
        }

        var bytes = new byte[bits.Count / 8];
        for (var i = 0; i < bytes.Length; i++)
        {
            byte value = 0;
            for (var b = 0; b < 8; b++)
                value = (byte)((value << 1) | bits[i * 8 + b]);
            bytes[i] = value;
        }

        result = bytes;
        return bytes.Length > 0;
    }

    public static byte[] DecodeBase32(string input)
    {
        if (!TryDecodeBase32(input, out var bytes))
            throw new ArgumentException("المفتاح Base32 غير صالح.");
        return bytes;
    }
}
