using System.Security.Cryptography;

namespace PasswordManager.Services;

public sealed class GeneratorOptions
{
    public int Length { get; set; } = 16;
    public bool UseLower { get; set; } = true;
    public bool UseUpper { get; set; } = true;
    public bool UseDigits { get; set; } = true;
    public bool UseSymbols { get; set; } = true;
    public bool ExcludeAmbiguous { get; set; } = false;
}

/// <summary>
/// مولد كلمات مرور قوية باستخدام RandomNumberGenerator (مصدر عشوائي آمن).
/// يضمن وجود حرف واحد على الأقل من كل نوع مختار، ويتجنب الأحرف المتشابهة عند الطلب.
/// </summary>
public static class PasswordGenerator
{
    public const int MinLength = 4;
    public const int MaxLength = 256;

    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>?/~";

    private const string Ambiguous = "Il1O0o`'\"";

    public static string Generate(GeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var length = Math.Clamp(options.Length, MinLength, MaxLength);
        var pools = new List<string>();

        if (options.UseLower) pools.Add(Lower);
        if (options.UseUpper) pools.Add(Upper);
        if (options.UseDigits) pools.Add(Digits);
        if (options.UseSymbols) pools.Add(Symbols);

        if (pools.Count == 0)
            throw new ArgumentException("يجب اختيار نوع واحد على الأقل من الأحرف.");

        var all = string.Concat(pools);
        if (options.ExcludeAmbiguous)
        {
            all = Filter(all);
            for (var i = 0; i < pools.Count; i++)
                pools[i] = Filter(pools[i]);

            pools.RemoveAll(string.IsNullOrEmpty);
            if (pools.Count == 0)
                throw new ArgumentException("بعد استثناء الأحرف المتشابهة لم يبقَ أي حرف صالح.");
        }

        // ضمان وجود حرف من كل مجموعة مختارة.
        var chars = new List<char>(length);
        foreach (var pool in pools)
            chars.Add(Pick(pool));

        while (chars.Count < length)
            chars.Add(Pick(all));

        // خلط عشوائي آمن.
        Shuffle(chars);
        return new string(chars.ToArray());
    }

    public static double EstimateBitsOfEntropy(GeneratorOptions options)
    {
        var poolSize = 0;
        if (options.UseLower) poolSize += Lower.Length;
        if (options.UseUpper) poolSize += Upper.Length;
        if (options.UseDigits) poolSize += Digits.Length;
        if (options.UseSymbols) poolSize += Symbols.Length;
        if (options.ExcludeAmbiguous)
            poolSize = (Filter(Lower).Length > 0 && options.UseLower ? Filter(Lower).Length : 0)
                     + (options.UseUpper ? Filter(Upper).Length : 0)
                     + (options.UseDigits ? Filter(Digits).Length : 0)
                     + (options.UseSymbols ? Filter(Symbols).Length : 0);

        return Math.Log2(poolSize) * options.Length;
    }

    private static char Pick(string pool)
    {
        var index = RandomNumberGenerator.GetInt32(pool.Length);
        return pool[index];
    }

    private static void Shuffle(List<char> chars)
    {
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }

    private static string Filter(string input)
    {
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var c in input)
            if (!Ambiguous.Contains(c, StringComparison.Ordinal))
                sb.Append(c);
        return sb.ToString();
    }
}
