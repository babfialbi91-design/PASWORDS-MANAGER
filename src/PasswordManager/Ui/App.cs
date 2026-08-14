using System.Security.Cryptography;
using PasswordManager.Models;
using PasswordManager.Services;
using Spectre.Console;

namespace PasswordManager.Ui;

public sealed class App
{
    private readonly VaultService _vault;
    private string _masterPassword = string.Empty;
    private VaultData _data = new();

    public App(string vaultPath)
    {
        _vault = new VaultService(vaultPath);
    }

    public async Task RunAsync()
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.Clear();
            Environment.Exit(0);
        };

        if (!_vault.Exists)
            await FirstRunAsync();
        else
            await UnlockAsync();

        await MainMenuAsync();
    }

    // ==================================================================
    //  الدخول الأول / فتح الخزنة
    // ==================================================================

    private async Task FirstRunAsync()
    {
        Ux.Header("إنشاء خزنة جديدة", "هذه أول مرة تشغّل فيها الأداة");

        AnsiConsole.MarkupLine(
            "[grey]سيتم إنشاء ملف مشفر على جهازك يحفظ كل بياناتك.\n" +
            "كلمة المرور الرئيسية هي [bold]المفتاح الوحيد[/] لفك التشفير — إذا نسيتها لن يستطيع أحد استرجاعها.[/]\n");

        string pw;
        while (true)
        {
            pw = Ux.AskSecret("أدخل كلمة المرور الرئيسية الجديدة:");
            if (pw.Length < 8)
            {
                Ux.Error("كلمة المرور يجب أن تكون 8 أحرف على الأقل.");
                continue;
            }
            var confirm = Ux.AskSecret("أعد كتابة كلمة المرور للتأكيد:");
            if (pw != confirm)
            {
                Ux.Error("كلمتا المرور غير متطابقتين، حاول مرة أخرى.");
                continue;
            }
            break;
        }

        _masterPassword = pw;
        _data = new VaultData();
        await _vault.CreateAsync(_masterPassword, _data);

        Ux.Success("تم إنشاء الخزنة وتشفيرها بنجاح.");
        Ux.Info($"موقع الملف: {_vault.VaultPath}");
        Ux.PressToContinue();
    }

    private async Task UnlockAsync()
    {
        Ux.Header("🔐 فتح الخزنة");

        while (true)
        {
            var pw = Ux.AskSecret("أدخل كلمة المرور الرئيسية:");
            if (string.IsNullOrEmpty(pw)) continue;

            try
            {
                _data = await _vault.OpenAsync(pw);
                _masterPassword = pw;
                Ux.Success("تم فتح الخزنة بنجاح.");
                break;
            }
            catch (CryptographicException)
            {
                Ux.Error("كلمة المرور غير صحيحة.");
                var reset = AnsiConsole.Confirm("هل نسيت كلمة المرور وتريد إعادة تعيين الخزنة؟ (سيتم حذف كل البيانات)", false);
                if (reset)
                {
                    ResetVault();
                    await FirstRunAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                Ux.Error($"تعذّر فتح الملف: {ex.Message}");
                var reset = AnsiConsole.Confirm("هل تريد إعادة تعيين الخزنة؟", false);
                if (reset)
                {
                    ResetVault();
                    await FirstRunAsync();
                    return;
                }
            }
        }
    }

    // ==================================================================
    //  القائمة الرئيسية
    // ==================================================================

    private async Task MainMenuAsync()
    {
        while (true)
        {
            Ux.Header("🔐 Password Manager", "مدير كلمات مرور آمن مع مولد قوي ورموز TOTP");

            AnsiConsole.MarkupLine(
                $"[grey]حالة الخزنة: [green]مفتوحة ✓[/] | الملف: {_vault.VaultPath.EscapeMarkup()}[/]\n");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]اختر من القائمة:[/]")
                    .PageSize(10)
                    .AddChoices(
                        "🔑  كلمات المرور",
                        "🎲  مولد كلمات المرور",
                        "⏱️  رموز TOTP",
                        "🔁  تغيير كلمة المرور الرئيسية",
                        "🗑️  إعادة تعيين الخزنة",
                        "❌  خروج"));

            switch (choice)
            {
                case "🔑  كلمات المرور":
                    await PasswordsMenuAsync();
                    break;
                case "🎲  مولد كلمات المرور":
                    await GeneratorMenuAsync();
                    break;
                case "⏱️  رموز TOTP":
                    await TotpMenuAsync();
                    break;
                case "🔁  تغيير كلمة المرور الرئيسية":
                    await ChangeMasterPasswordAsync();
                    break;
                case "🗑️  إعادة تعيين الخزنة":
                    await ResetVaultConfirmAsync();
                    break;
                default:
                    Console.Clear();
                    AnsiConsole.MarkupLine("[grey]وداعاً... لا تنسَ كلمة مرورك الرئيسية![/]");
                    return;
            }
        }
    }

    // ==================================================================
    //  إدارة كلمات المرور
    // ==================================================================

    private async Task PasswordsMenuAsync()
    {
        while (true)
        {
            Ux.Header("🔑 كلمات المرور", $"{_data.Passwords.Count} مدخل محفوظ");

            if (_data.Passwords.Count == 0)
            {
                Ux.Info("لا توجد كلمات مرور محفوظة بعد.");
            }
            else
            {
                var table = new Table().Border(TableBorder.Rounded).Expand();
                table.AddColumns("[bold]#[/]", "[bold]العنوان[/]", "[bold]اسم المستخدم[/]", "[bold]الموقع[/]", "[bold]القوة[/]", "[bold]آخر تعديل[/]");

                var index = 1;
                foreach (var entry in _data.Passwords.OrderByDescending(e => e.UpdatedAt))
                {
                    table.AddRow(
                        index.ToString(),
                        Ux.SafeText(entry.Title),
                        Ux.SafeText(entry.Username),
                        Ux.SafeText(entry.Website),
                        $"[{Ux.StrengthColor(entry.Password)}]{Ux.StrengthLabel(entry.Password)}[/]",
                        entry.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd"));
                    index++;
                }

                AnsiConsole.Write(table);
            }

            AnsiConsole.WriteLine();
            var choices = new List<string> { "➕  إضافة كلمة مرور", "🔍  بحث", "🔙  رجوع" };
            if (_data.Passwords.Count > 0)
                choices.Insert(0, "👁️  عرض مدخل وتفاصيله");

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]ماذا تريد أن تفعل؟[/]")
                    .AddChoices(choices));

            switch (action)
            {
                case "➕  إضافة كلمة مرور":
                    await AddPasswordAsync();
                    break;
                case "🔍  بحث":
                    SearchPasswords();
                    break;
                case "👁️  عرض مدخل وتفاصيله":
                    await ShowPasswordEntryAsync();
                    break;
                default:
                    return;
            }
        }
    }

    private async Task AddPasswordAsync()
    {
        Ux.Header("➕ إضافة كلمة مرور جديدة");

        var entry = new PasswordEntry
        {
            Title = Ux.Ask("العنوان أو اسم الموقع:", "موقعي"),
            Username = Ux.Ask("اسم المستخدم / البريد الإلكتروني:"),
            Website = Ux.Ask("رابط الموقع (اختياري):"),
            Category = Ux.Ask("التصنيف (اختياري):", "عام"),
            Notes = Ux.Ask("ملاحظات (اختياري):")
        };

        if (Ux.Confirm("هل تريد توليد كلمة مرور قوية تلقائياً؟", true))
        {
            entry.Password = RunGeneratorForPassword();
            if (string.IsNullOrEmpty(entry.Password))
                return;
        }
        else
        {
            entry.Password = Ux.AskSecret("أدخل كلمة المرور:");
            AnsiConsole.MarkupLine($"قوة كلمة المرور: [bold {Ux.StrengthColor(entry.Password)}]{Ux.StrengthLabel(entry.Password)}[/]");
        }

        _data.Passwords.Add(entry);
        await SaveAsync();

        Ux.Success($"تم حفظ \"{entry.Title}\".");
        if (Ux.Confirm("نسخ كلمة المرور إلى الحافظة؟", false))
            CopyToClipboard(entry.Password);
    }

    private void SearchPasswords()
    {
        Ux.Header("🔍 بحث");

        var term = Ux.Ask("أدخل نص البحث (العنوان / المستخدم / الموقع):").Trim();
        if (string.IsNullOrEmpty(term)) return;

        var results = _data.Passwords
            .Where(e =>
                e.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Username.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Website.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Notes.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (results.Count == 0)
        {
            Ux.Warn($"لا توجد نتائج تطابق \"{term}\".");
            Ux.PressToContinue();
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumns("[bold]العنوان[/]", "[bold]المستخدم[/]", "[bold]الموقع[/]", "[bold]القوة[/]");
        foreach (var e in results)
            table.AddRow(Ux.SafeText(e.Title), Ux.SafeText(e.Username), Ux.SafeText(e.Website),
                $"[{Ux.StrengthColor(e.Password)}]{Ux.StrengthLabel(e.Password)}[/]");
        AnsiConsole.Write(table);
        Ux.PressToContinue();
    }

    private async Task ShowPasswordEntryAsync()
    {
        if (_data.Passwords.Count == 0) return;

        var entry = PickEntry();
        if (entry is null) return;

        while (true)
        {
            Ux.Header($"👁️ {entry.Title}", entry.Website);

            var table = new Table().Border(TableBorder.Rounded).Expand();
            table.AddColumn("[bold]الحقل[/]");
            table.AddColumn("[bold]القيمة[/]");
            table.AddRow("[bold]العنوان[/]", Ux.SafeText(entry.Title));
            table.AddRow("[bold]اسم المستخدم[/]", Ux.SafeText(entry.Username));
            table.AddRow("[bold]الموقع[/]", Ux.SafeText(entry.Website));
            table.AddRow("[bold]التصنيف[/]", Ux.SafeText(entry.Category));
            table.AddRow("[bold]كلمة المرور[/]",
                $"[{Ux.StrengthColor(entry.Password)}]{new string('•', Math.Max(4, Math.Min(16, entry.Password.Length)))}[/] ({Ux.StrengthLabel(entry.Password)})");
            table.AddRow("[bold]ملاحظات[/]", Ux.SafeText(entry.Notes));
            table.AddRow("[bold]أُنشئت[/]", entry.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
            AnsiConsole.Write(table);

            AnsiConsole.WriteLine();
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]اختر إجراءً:[/]")
                    .AddChoices(
                        "📋  نسخ كلمة المرور",
                        "👤  نسخ اسم المستخدم",
                        "👁️  إظهار كلمة المرور",
                        "✏️  تعديل",
                        "🗑️  حذف",
                        "🔙  رجوع"));

            switch (action)
            {
                case "📋  نسخ كلمة المرور":
                    CopyToClipboard(entry.Password);
                    break;
                case "👤  نسخ اسم المستخدم":
                    CopyToClipboard(entry.Username);
                    break;
                case "👁️  إظهار كلمة المرور":
                    AnsiConsole.MarkupLine($"\n[bold]{entry.Password.EscapeMarkup()}[/]\n");
                    Ux.PressToContinue();
                    break;
                case "✏️  تعديل":
                    await EditPasswordEntryAsync(entry);
                    break;
                case "🗑️  حذف":
                    if (Ux.Confirm($"هل تريد حذف \"{entry.Title}\" نهائياً؟", false))
                    {
                        _data.Passwords.Remove(entry);
                        await SaveAsync();
                        Ux.Success("تم الحذف.");
                        Ux.PressToContinue();
                    }
                    return;
                default:
                    return;
            }
        }
    }

    private async Task EditPasswordEntryAsync(PasswordEntry entry)
    {
        var field = Ux.Pick("أي حقل تريد تعديله؟", new[]
        {
            $"العنوان (الحالي: {entry.Title})",
            $"اسم المستخدم (الحالي: {entry.Username})",
            $"الموقع (الحالي: {entry.Website})",
            $"التصنيف (الحالي: {entry.Category})",
            "كلمة المرور",
            "الملاحظات"
        });

        switch (field)
        {
            case string f when f.StartsWith("العنوان"):
                entry.Title = Ux.Ask("العنوان الجديد:", entry.Title);
                break;
            case string f when f.StartsWith("اسم المستخدم"):
                entry.Username = Ux.Ask("اسم المستخدم الجديد:", entry.Username);
                break;
            case string f when f.StartsWith("الموقع"):
                entry.Website = Ux.Ask("الموقع الجديد:", entry.Website);
                break;
            case string f when f.StartsWith("التصنيف"):
                entry.Category = Ux.Ask("التصنيف الجديد:", entry.Category);
                break;
            case "كلمة المرور":
                if (Ux.Confirm("توليد كلمة مرور جديدة تلقائياً؟", true))
                {
                    var pw = RunGeneratorForPassword();
                    if (string.IsNullOrEmpty(pw)) return;
                    entry.Password = pw;
                }
                else
                {
                    entry.Password = Ux.AskSecret("كلمة المرور الجديدة:");
                }
                break;
            default:
                entry.Notes = Ux.Ask("الملاحظات الجديدة:", entry.Notes);
                break;
        }

        entry.UpdatedAt = DateTime.UtcNow;
        await SaveAsync();
        Ux.Success("تم حفظ التعديلات.");
        Ux.PressToContinue();
    }

    private PasswordEntry? PickEntry()
    {
        if (_data.Passwords.Count == 0) return null;

        var entries = _data.Passwords
            .OrderByDescending(e => e.UpdatedAt)
            .Select(e =>
            {
                var label = string.IsNullOrEmpty(e.Website)
                    ? $"{e.Title}  ( {e.Username} )"
                    : $"{e.Title}  ( {e.Username} )  — {e.Website}";
                return (Label: label, Entry: e);
            })
            .ToList();

        var picked = Ux.Pick("اختر مدخلاً:", entries.Select(x => x.Label).Append("🔙  رجوع"));
        if (picked == "🔙  رجوع") return null;

        return entries.First(x => x.Label == picked).Entry;
    }

    // ==================================================================
    //  مولد كلمات المرور
    // ==================================================================

    private async Task GeneratorMenuAsync()
    {
        var options = new GeneratorOptions();

        while (true)
        {
            Ux.Header("🎲 مولد كلمات المرور", "اقوى كلمات المرور بخياراتك الكاملة");

            AnsiConsole.MarkupLine(
                $"الطول: [bold]{options.Length}[/] | " +
                $"أحرف كبيرة: [bold]{(options.UseUpper ? "✓" : "✗")}[/] | " +
                $"أحرف صغيرة: [bold]{(options.UseLower ? "✓" : "✗")}[/] | " +
                $"أرقام: [bold]{(options.UseDigits ? "✓" : "✗")}[/] | " +
                $"رموز: [bold]{(options.UseSymbols ? "✓" : "✗")}[/] | " +
                $"استثناء المتشابهة: [bold]{(options.ExcludeAmbiguous ? "✓" : "✗")}[/]\n");

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]ماذا تريد أن تفعل؟[/]")
                    .AddChoices(
                        "⚡  توليد كلمة مرور",
                        "🔧  تغيير الخيارات",
                        "➕  توليد وحفظ لموقع محدد",
                        "🔙  رجوع"));

            switch (action)
            {
                case "⚡  توليد كلمة مرور":
                {
                    var password = GenerateAndShow(options);
                    if (string.IsNullOrEmpty(password)) break;
                    if (Ux.Confirm("نسخ كلمة المرور إلى الحافظة؟", false))
                        CopyToClipboard(password);
                    break;
                }
                case "🔧  تغيير الخيارات":
                    options = EditGeneratorOptions(options);
                    break;
                case "➕  توليد وحفظ لموقع محدد":
                    await GenerateAndSaveAsync(options);
                    break;
                default:
                    return;
            }
        }
    }

    private GeneratorOptions EditGeneratorOptions(GeneratorOptions current)
    {
        var options = new GeneratorOptions
        {
            Length = AnsiConsole.Prompt(
                new TextPrompt<int>($"الطول ({PasswordGenerator.MinLength}-{PasswordGenerator.MaxLength}):")
                    .DefaultValue(current.Length)
                    .ValidationErrorMessage("أدخل رقماً صحيحاً.")
                    .Validate(l => l is >= PasswordGenerator.MinLength and <= PasswordGenerator.MaxLength)),
            UseUpper = AnsiConsole.Confirm("أحرف كبيرة (A-Z)؟", current.UseUpper),
            UseLower = AnsiConsole.Confirm("أحرف صغيرة (a-z)؟", current.UseLower),
            UseDigits = AnsiConsole.Confirm("أرقام (0-9)؟", current.UseDigits),
            UseSymbols = AnsiConsole.Confirm("رموز خاصة (!@#$)؟", current.UseSymbols),
            ExcludeAmbiguous = AnsiConsole.Confirm("استثناء الأحرف المتشابهة (I, l, 1, O, 0)؟", current.ExcludeAmbiguous)
        };
        return options;
    }

    private string GenerateAndShow(GeneratorOptions options)
    {
        try
        {
            var password = PasswordGenerator.Generate(options);
            var entropy = PasswordGenerator.EstimateBitsOfEntropy(options);

            Ux.Header("🎲 كلمة المرور المولّدة");
            var panel = new Panel($"[bold yellow]{password.EscapeMarkup()}[/]")
                .Header("كلمة المرور", Justify.Center)
                .Border(BoxBorder.Rounded)
                .Padding(1, 1);
            AnsiConsole.Write(panel);

            AnsiConsole.MarkupLine(
                $"[grey]القوة: [bold {Ux.StrengthColor(password)}]{Ux.StrengthLabel(password)}[/] | " +
                $"الانتروبيا التقريبية: [bold]{entropy:0} بت[/][/]\n");

            return password;
        }
        catch (Exception ex)
        {
            Ux.Error(ex.Message);
            Ux.PressToContinue();
            return string.Empty;
        }
    }

    private string RunGeneratorForPassword()
    {
        var options = new GeneratorOptions { Length = 18 };
        var password = GenerateAndShow(options);
        if (string.IsNullOrEmpty(password)) return string.Empty;
        return password;
    }

    private async Task GenerateAndSaveAsync(GeneratorOptions options)
    {
        var password = GenerateAndShow(options);
        if (string.IsNullOrEmpty(password)) return;

        AnsiConsole.WriteLine();
        if (!Ux.Confirm("حفظ هذه الكلمة في الخزنة؟", true)) return;

        var entry = new PasswordEntry
        {
            Title = Ux.Ask("العنوان أو اسم الموقع:"),
            Username = Ux.Ask("اسم المستخدم / البريد الإلكتروني (اختياري):"),
            Website = Ux.Ask("رابط الموقع:"),
            Category = Ux.Ask("التصنيف (اختياري):", "عام"),
            Notes = Ux.Ask("ملاحظات (اختياري):"),
            Password = password
        };

        _data.Passwords.Add(entry);
        await SaveAsync();
        Ux.Success($"تم حفظ كلمة المرور للموقع \"{entry.Title}\".");

        if (Ux.Confirm("نسخ كلمة المرور إلى الحافظة؟", false))
            CopyToClipboard(password);
    }

    // ==================================================================
    //  TOTP
    // ==================================================================

    private async Task TotpMenuAsync()
    {
        while (true)
        {
            Ux.Header("⏱️ رموز TOTP", "رموز المصادقة الثنائية التي تتغير كل 30 ثانية");

            if (_data.TotpAccounts.Count == 0)
            {
                Ux.Info("لا توجد حسابات TOTP مضافة بعد.");
                AnsiConsole.WriteLine();
            }
            else
            {
                var table = new Table().Border(TableBorder.Rounded).Expand();
                table.AddColumns("[bold]#[/]", "[bold]الحساب[/]", "[bold]الرمز الحالي[/]", "[bold]ينتهي بعد[/]");

                var index = 1;
                foreach (var account in _data.TotpAccounts.OrderBy(a => a.Name))
                {
                    try
                    {
                        var code = TotpService.ComputeCode(account, DateTimeOffset.Now);
                        var remaining = TotpService.SecondsRemaining(account, DateTimeOffset.Now);
                        table.AddRow(
                            index.ToString(),
                            Ux.SafeText(account.Name),
                            $"[bold yellow]{code}[/]",
                            $"[{(remaining <= 5 ? "red" : "green")}]{remaining}s[/]");
                    }
                    catch
                    {
                        table.AddRow(index.ToString(), Ux.SafeText(account.Name), "[red]مفتاح غير صالح[/]", "—");
                    }
                    index++;
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }

            var choices = new List<string> { "➕  إضافة حساب", "🔙  رجوع" };
            if (_data.TotpAccounts.Count > 0)
                choices.InsertRange(0, new[] { "⏱️  مراقبة الرموز (عدّاد مباشر)", "📋  نسخ رمز حساب", "🗑️  حذف حساب" });

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]ماذا تريد أن تفعل؟[/]")
                    .AddChoices(choices));

            switch (action)
            {
                case "➕  إضافة حساب":
                    await AddTotpAccountAsync();
                    break;
                case "⏱️  مراقبة الرموز (عدّاد مباشر)":
                    WatchTotpCodes();
                    break;
                case "📋  نسخ رمز حساب":
                    CopyTotpCode();
                    break;
                case "🗑️  حذف حساب":
                    await DeleteTotpAccountAsync();
                    break;
                default:
                    return;
            }
        }
    }

    private async Task AddTotpAccountAsync()
    {
        Ux.Header("➕ إضافة حساب TOTP");

        AnsiConsole.MarkupLine(
            "[grey]الصق المفتاح السري (Secret) من الموقع — مثال: [bold]JBSWY3DPEHPK3PXP[/]\n" +
            "أو الصق رابط otpauth:// كاملاً إذا كان الموقع يوفر خيار مسح QR.[/]\n");

        var input = Ux.Ask("المفتاح السري أو رابط otpauth:");
        var parsed = TotpService.ParseInput(input, string.Empty);

        if (parsed is null)
        {
            Ux.Error("تعذّر قراءة المفتاح. تأكد أنك نسخته كاملاً.");
            Ux.PressToContinue();
            return;
        }

        if (parsed.Name == "حساب بدون اسم" || parsed.Name.StartsWith("حساب بدون اسم"))
            parsed.Name = Ux.Ask("اسم الحساب (مثال: Google, GitHub):");

        // إذا لم يأتِ من رابط otpauth، اترك الخيارات الافتراضية.
        if (!input.TrimStart().StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
        {
            parsed.Algorithm = Ux.Pick("خوارزمية التجزئة:", TotpService.SupportedAlgorithms);
            parsed.Digits = Ux.Pick("عدد خانات الرمز:", TotpService.SupportedDigits);
        }

        // معاينة قبل الحفظ
        try
        {
            var preview = TotpService.ComputeCode(parsed, DateTimeOffset.Now);
            AnsiConsole.MarkupLine($"\n[grey]الرمز الحالي لهذا الحساب: [bold yellow]{preview}[/][/]\n");
        }
        catch (Exception ex)
        {
            Ux.Error($"المفتاح غير صالح: {ex.Message}");
            Ux.PressToContinue();
            return;
        }

        if (!Ux.Confirm("تأكيد الحفظ؟", true)) return;

        _data.TotpAccounts.Add(parsed);
        await SaveAsync();
        Ux.Success($"تمت إضافة الحساب \"{parsed.Name}\".");
        Ux.PressToContinue();
    }

    private void WatchTotpCodes()
    {
        while (true)
        {
            try
            {
                if (Console.KeyAvailable)
                {
                    while (Console.KeyAvailable) Console.ReadKey(true);
                    break;
                }
            }
            catch (InvalidOperationException)
            {
                // إدخال مُعاد توجيهه — لا يمكن تتبع الضغطات، نخرج بعد مدة.
                Thread.Sleep(500);
                break;
            }

            Ux.Header("⏱️ المراقبة المباشرة", "اضغط أي مفتاح للخروج");

            var table = new Table().Border(TableBorder.Rounded).Expand();
            table.AddColumns("[bold]الحساب[/]", "[bold]الرمز الحالي[/]", "[bold]الرمز التالي[/]", "[bold]المتبقي[/]");

            foreach (var account in _data.TotpAccounts.OrderBy(a => a.Name))
            {
                var now = DateTimeOffset.Now;
                var remaining = TotpService.SecondsRemaining(account, now);
                try
                {
                    var current = TotpService.ComputeCode(account, now);
                    var next = TotpService.ComputeCode(account, now.AddSeconds(remaining));
                    var filled = Math.Clamp((int)Math.Round((double)remaining / account.Period * 15), 0, 15);
                    var bar = $"[{(remaining <= 5 ? "red" : "green")}]{new string('█', filled)}{new string('░', 15 - filled)}[/]";

                    table.AddRow(Ux.SafeText(account.Name), $"[bold yellow]{current}[/]", $"[grey]{next}[/]", bar);
                }
                catch
                {
                    table.AddRow(Ux.SafeText(account.Name), "[red]مفتاح غير صالح[/]", "—", "—");
                }
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("\n[grey](اضغط أي مفتاح للخروج من المراقبة)[/]");

            Thread.Sleep(500);
        }
    }

    private void CopyTotpCode()
    {
        if (_data.TotpAccounts.Count == 0) return;

        var accounts = _data.TotpAccounts.OrderBy(a => a.Name).ToList();
        var label = Ux.Pick("اختر الحساب:", accounts.Select(a => a.Name).Append("🔙  رجوع"));
        if (label == "🔙  رجوع") return;

        var account = accounts.First(a => a.Name == label);
        try
        {
            var code = TotpService.ComputeCode(account, DateTimeOffset.Now);
            CopyToClipboard(code);
        }
        catch (Exception ex)
        {
            Ux.Error(ex.Message);
            Ux.PressToContinue();
        }
    }

    private async Task DeleteTotpAccountAsync()
    {
        if (_data.TotpAccounts.Count == 0) return;

        var accounts = _data.TotpAccounts.OrderBy(a => a.Name).ToList();
        var label = Ux.Pick("اختر الحساب للحذف:", accounts.Select(a => a.Name).Append("🔙  رجوع"));
        if (label == "🔙  رجوع") return;

        var account = accounts.First(a => a.Name == label);
        if (!Ux.Confirm($"حذف حساب \"{account.Name}\" نهائياً؟", false)) return;

        _data.TotpAccounts.Remove(account);
        await SaveAsync();
        Ux.Success("تم الحذف.");
        Ux.PressToContinue();
    }

    // ==================================================================
    //  إعدادات الخزنة
    // ==================================================================

    private async Task ChangeMasterPasswordAsync()
    {
        Ux.Header("🔁 تغيير كلمة المرور الرئيسية");

        var oldPw = Ux.AskSecret("كلمة المرور الحالية:");
        try
        {
            var check = await _vault.OpenAsync(oldPw);
        }
        catch (CryptographicException)
        {
            Ux.Error("كلمة المرور الحالية غير صحيحة.");
            Ux.PressToContinue();
            return;
        }

        string newPw;
        while (true)
        {
            newPw = Ux.AskSecret("كلمة المرور الجديدة:");
            if (newPw.Length < 8)
            {
                Ux.Error("كلمة المرور يجب أن تكون 8 أحرف على الأقل.");
                continue;
            }
            var confirm = Ux.AskSecret("أعد كتابة كلمة المرور الجديدة:");
            if (newPw != confirm)
            {
                Ux.Error("غير متطابقتين، حاول مجدداً.");
                continue;
            }
            break;
        }

        await _vault.ChangeMasterPasswordAsync(oldPw, newPw);
        _masterPassword = newPw;
        Ux.Success("تم تغيير كلمة المرور الرئيسية بنجاح.");
        Ux.PressToContinue();
    }

    private async Task ResetVaultConfirmAsync()
    {
        Ux.Header("🗑️ إعادة تعيين الخزنة");

        AnsiConsole.MarkupLine("[red bold]تحذير: سيتم حذف كل كلمات المرور وحسابات TOTP نهائياً ولا يمكن استرجاعها.[/]\n");
        if (!Ux.Confirm("هل أنت متأكد تماماً؟", false)) return;

        if (Ux.Ask("اكتب [bold]RESET[/] للتأكيد:") == "RESET")
        {
            ResetVault();
            await FirstRunAsync();
        }
    }

    private void ResetVault()
    {
        _vault.Reset();
        _data = new VaultData();
        _masterPassword = string.Empty;
    }

    // ==================================================================
    //  أدوات مساعدة
    // ==================================================================

    private void CopyToClipboard(string text)
    {
        if (ClipboardService.TryCopy(text))
            Ux.Success("تم النسخ إلى الحافظة.");
        else
            Ux.Error("تعذّر النسخ إلى الحافظة.");
        Ux.PressToContinue();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(_masterPassword)) return;
        await _vault.SaveAsync(_masterPassword, _data);
    }
}
