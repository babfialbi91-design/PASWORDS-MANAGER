using System.Text;
using PasswordManager.Services;
using PasswordManager.Ui;

namespace PasswordManager;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        if (args.Length > 0 && args[0] == "--selftest")
            return SelfTest.Run();

        if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
        {
            PrintHelp();
            return 0;
        }

        // مسار الخزنة: مسار مخصص من سطر الأوامر، أو متغير بيئة، أو المجلد الافتراضي.
        var vaultPath = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Environment.GetEnvironmentVariable("PM_VAULT_PATH");

        if (string.IsNullOrEmpty(vaultPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            vaultPath = Path.Combine(appData, "PasswordManager", "vault.dat");
        }

        try
        {
            var app = new App(vaultPath);
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"خطأ غير متوقع: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Password Manager — مدير كلمات المرور");
        Console.WriteLine();
        Console.WriteLine("الاستخدام:  pm [path-to-vault] | --selftest | --help");
        Console.WriteLine();
        Console.WriteLine("  <ملف الخزنة>   مسار مخصص لملف الخزنة المشفر (افتراضياً %APPDATA%\\PasswordManager\\vault.dat)");
        Console.WriteLine("  --selftest     تشغيل اختبارات ذاتية على التشفير والمولد ورموز TOTP");
        Console.WriteLine("  --help         عرض هذه المساعدة");
        Console.WriteLine();
        Console.WriteLine("متغير بيئة PM_VAULT_PATH يحدد مسار الخزنة أيضاً.");
    }
}

public static class SelfTest
{
    public static int Run()
    {
        Console.WriteLine("══════ Password Manager — الاختبار الذاتي ══════");
        var pass = 0;
        var fail = 0;

        void Check(string name, bool ok)
        {
            if (ok)
            {
                pass++;
                Console.WriteLine($"  [OK]   {name}");
            }
            else
            {
                fail++;
                Console.WriteLine($"  [FAIL] {name}");
            }
        }

        Console.WriteLine("\n── التشفير (AES-256-GCM + PBKDF2) ──");
        try
        {
            var pw = "TestMasterPassword123!";
            var salt = CryptoService.GenerateSalt();
            var key = CryptoService.DeriveKey(pw, salt, CryptoService.Iterations);
            var plaintext = Encoding.UTF8.GetBytes("Hello secure vault!");
            var (nonce, tag, ct) = CryptoService.Encrypt(key, plaintext);
            var dec = CryptoService.Decrypt(key, nonce, tag, ct);
            Check("AES-GCM تشفير وفك (roundtrip)", Encoding.UTF8.GetString(dec) == "Hello secure vault!");

            var wrongKey = CryptoService.DeriveKey("wrong-password", salt, CryptoService.Iterations);
            var threw = false;
            try { CryptoService.Decrypt(wrongKey, nonce, tag, ct); } catch { threw = true; }
            Check("رفض المفتاح الخاطئ", threw);

            // اختبار دورة الخزنة كاملة
            var tmpVault = Path.Combine(Path.GetTempPath(), $"pm_selftest_{Guid.NewGuid():N}.dat");
            try
            {
                var svc = new VaultService(tmpVault);
                var data = new Models.VaultData();
                data.Passwords.Add(new Models.PasswordEntry { Title = "اختبار", Username = "u", Password = "p@ss" });
                svc.CreateAsync(pw, data).GetAwaiter().GetResult();

                var opened = svc.OpenAsync(pw).GetAwaiter().GetResult();
                Check("إنشاء وفتح الخزنة", opened.Passwords.Count == 1 && opened.Passwords[0].Title == "اختبار");

                var bad = false;
                try { svc.OpenAsync("wrong").GetAwaiter().GetResult(); } catch { bad = true; }
                Check("فشل الفتح بكلمة مرور خاطئة", bad);

                opened.Passwords[0].Password = "new-pass";
                svc.SaveAsync(pw, opened).GetAwaiter().GetResult();
                var reopened = svc.OpenAsync(pw).GetAwaiter().GetResult();
                Check("الحفظ وإعادة الفتح", reopened.Passwords[0].Password == "new-pass");

                svc.ChangeMasterPasswordAsync(pw, "NewMaster999").GetAwaiter().GetResult();
                var re2 = svc.OpenAsync("NewMaster999").GetAwaiter().GetResult();
                Check("تغيير كلمة المرور الرئيسية", re2.Passwords.Count == 1);

                var raw = File.ReadAllText(tmpVault);
                Check("المحتوى مشفر (لا يحتوي كلمة المرور نصياً)", !raw.Contains("new-pass") && !raw.Contains("p@ss"));
            }
            finally
            {
                if (File.Exists(tmpVault)) File.Delete(tmpVault);
            }
        }
        catch (Exception ex)
        {
            Check($"تشفير (استثناء: {ex.Message})", false);
        }

        Console.WriteLine("\n── مولد كلمات المرور ──");
        try
        {
            var opts = new GeneratorOptions { Length = 32, UseLower = true, UseUpper = true, UseDigits = true, UseSymbols = true };
            var p1 = PasswordGenerator.Generate(opts);
            Check($"طول 32 بنوعية كاملة: {p1.Length} حرفاً", p1.Length == 32 && p1.Any(char.IsUpper) && p1.Any(char.IsLower) && p1.Any(char.IsDigit) && p1.Any(c => !char.IsLetterOrDigit(c)));

            var p2 = PasswordGenerator.Generate(new GeneratorOptions { Length = 12, UseLower = false, UseUpper = false, UseDigits = true, UseSymbols = false, ExcludeAmbiguous = true });
            Check("أرقام فقط 12 خانة", p2.Length == 12 && p2.All(char.IsDigit));

            var p3 = PasswordGenerator.Generate(new GeneratorOptions { Length = 20, UseLower = true, UseUpper = true, UseDigits = true, UseSymbols = true, ExcludeAmbiguous = true });
            var ambiguous = "Il1O0o`'\"";
            Check("استثناء الأحرف المتشابهة", p3.All(c => !ambiguous.Contains(c, StringComparison.Ordinal)));

            var many = Enumerable.Range(0, 200).Select(_ => PasswordGenerator.Generate(opts)).Distinct().Count();
            Check("عشوائية (200 توليدات مختلفة)", many >= 195);
        }
        catch (Exception ex)
        {
            Check($"مولد (استثناء: {ex.Message})", false);
        }

        Console.WriteLine("\n── TOTP (RFC 6238 vectors) ──");
        try
        {
            // RFC 6238: مفتاح SHA1 هو ASCII "12345678901234567890"،
            // بينما SHA256/SHA512 تستخدمان مفاتيح أطول (32 و64 بايت).
            var secretSha1 = Base32Encode(Encoding.ASCII.GetBytes("12345678901234567890"));
            var secretSha256 = Base32Encode(Encoding.ASCII.GetBytes("12345678901234567890123456789012"));
            var secretSha512 = Base32Encode(Encoding.ASCII.GetBytes("1234567890123456789012345678901234567890123456789012345678901234"));

            var vectors = new[]
            {
                (T: 59L, S: secretSha1,   A: "SHA1",   E: "94287082"),
                (T: 59L, S: secretSha256, A: "SHA256", E: "46119246"),
                (T: 59L, S: secretSha512, A: "SHA512", E: "90693936"),
                (T: 1111111109L, S: secretSha1,   A: "SHA1",   E: "07081804"),
                (T: 1111111109L, S: secretSha256, A: "SHA256", E: "68084774"),
                (T: 1111111109L, S: secretSha512, A: "SHA512", E: "25091201"),
                (T: 1111111111L, S: secretSha1,   A: "SHA1",   E: "14050471"),
                (T: 1234567890L, S: secretSha1,   A: "SHA1",   E: "89005924"),
            };

            var ok = true;
            foreach (var v in vectors)
            {
                var actual = TotpService.ComputeCode(v.S, v.A, 8, 30, DateTimeOffset.FromUnixTimeSeconds(v.T));
                if (actual != v.E)
                {
                    ok = false;
                    Console.WriteLine($"      mismatch t={v.T} {v.A}: got {actual} expected {v.E}");
                }
            }
            Check("جميع ناقلات RFC 6238 (SHA1/256/512)", ok);

            // otpauth parsing
            var acc = TotpService.ParseInput("otpauth://totp/GitHub:user?secret=JBSWY3DPEHPK3PXP&algorithm=SHA256&digits=6&period=30", "");
            Check("تحليل رابط otpauth://", acc is not null && acc.Name == "GitHub - user" && acc.Algorithm == "SHA256" && acc.Digits == 6 && acc.Period == 30);

            var acc2 = TotpService.ParseInput("JBSWY3DPEHPK3PXP", "");
            Check("قراءة مفتاح Base32 مباشرة", acc2 is not null && acc2.SecretBase32 == "JBSWY3DPEHPK3PXP");
            Check("رفض مفتاح غير صالح", TotpService.ParseInput("NOT*VALID!!", "") is null);
        }
        catch (Exception ex)
        {
            Check($"TOTP (استثناء: {ex.Message})", false);
        }

        Console.WriteLine($"\n═══════════════════════════════════════════════");
        Console.WriteLine(fail == 0
            ? $"النتيجة: [green]نجح كل شيء — {pass} فحصاً[/]"
            : $"النتيجة: {pass} ناجح، {fail} فاشل");
        return fail == 0 ? 0 : 1;
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sb = new StringBuilder();
        var bits = 0;
        var value = 0;
        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(alphabet[(value >> (bits - 5)) & 0x1F]);
                bits -= 5;
            }
        }
        if (bits > 0)
            sb.Append(alphabet[(value << (5 - bits)) & 0x1F]);
        return sb.ToString();
    }
}
