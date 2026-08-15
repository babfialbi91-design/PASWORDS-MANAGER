using System.ComponentModel;
using System.Runtime.CompilerServices;
using PasswordManager.Services;

namespace PasswordManager.App;

public enum Language
{
    Arabic,
    English
}

/// <summary>
/// خدمة ترجمة الواجهة بين العربية والإنجليزية مع حفظ الاختيار.
/// تستخدمها الواجهة عبر Bindings للفهرسة: {Binding [Key], Source={x:Static loc:Localization.Instance}}
/// </summary>
public sealed class Localization : INotifyPropertyChanged
{
    public static Localization Instance { get; } = new();

    public static event Action? LanguageChanged;

    private Language _language = Language.Arabic;

    public Language Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            _language = value;
            new AppSettings { Language = value }.Save();
            LanguageChanged?.Invoke();
            OnPropertyChanged(nameof(Language));
            OnPropertyChanged("Item[]");
        }
    }

    public bool IsRtl => _language == Language.Arabic;

    public string this[string key] => Get(key);

    public static string Get(string key)
    {
        var i = Instance;
        if (i._language == Language.English && En.TryGetValue(key, out var en)) return en;
        return Ar.TryGetValue(key, out var ar) ? ar : key;
    }

    public static string Strength(PasswordStrength strength) => Get("Strength_" + strength);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static readonly Dictionary<string, string> Ar = new()
    {
        ["App_Title"] = "مدير كلمات المرور",
        ["Main_SaveError"] = "تعذّر حفظ الخزنة: {0}",

        ["Sidebar_VaultOpen"] = "الخزنة مفتوحة",
        ["Sidebar_VaultFile"] = "ملف الخزنة",
        ["Sidebar_Lock"] = "🔒  قفل الخزنة",
        ["Sidebar_NavPasswords"] = "🔑  كلمات المرور",
        ["Sidebar_NavGenerator"] = "🎲  مولد كلمات المرور",
        ["Sidebar_NavTotp"] = "⏱️  رموز TOTP",
        ["Sidebar_NavSettings"] = "⚙️  الإعدادات",

        ["Update_BarNew"] = "يتوفر تحديث جديد: الإصدار {0}",
        ["Update_BarGeneric"] = "يتوفر تحديث جديد",
        ["Update_Download"] = "تحميل التحديث",

        ["Common_Error"] = "خطأ",
        ["Common_Cancel"] = "إلغاء",
        ["Common_Copy"] = "📋 نسخ",
        ["Common_Delete"] = "🗑️ حذف",
        ["Common_Copied"] = "✓ نُسخت",
        ["Common_ConfirmDelete"] = "تأكيد الحذف",
        ["Common_ClipboardFailed"] = "تعذّر النسخ إلى الحافظة.",
        ["Common_Notice"] = "تنبيه",

        ["Strength_Empty"] = "فارغة",
        ["Strength_Weak"] = "ضعيفة",
        ["Strength_Medium"] = "متوسطة",
        ["Strength_Strong"] = "قوية",
        ["Strength_VeryStrong"] = "قوية جداً",

        ["Login_PasswordLabel"] = "كلمة المرور الرئيسية (8 أحرف على الأقل)",
        ["Login_ShowPassword"] = "إظهار كلمة المرور",
        ["Login_ConfirmLabel"] = "إعادة كتابة كلمة المرور للتأكيد",
        ["Login_TitleSetup"] = "إنشاء خزنة جديدة",
        ["Login_SubSetup"] = "هذه أول مرة تستخدم فيها الأداة — حدد كلمة مرور رئيسية تُشفّر بها كل بياناتك. إن نسيتها لن يستطيع أحد استرجاعها.",
        ["Login_ActionSetup"] = "إنشاء الخزنة",
        ["Login_TitleUnlock"] = "فتح الخزنة",
        ["Login_SubUnlock"] = "أدخل كلمة المرور الرئيسية لفتح بياناتك.",
        ["Login_ActionUnlock"] = "دخول",
        ["Login_ResetLink"] = "نسيت كلمة المرور؟  إعادة تعيين الخزنة",
        ["Login_Strength"] = "القوة: {0}",
        ["Login_Match"] = "✓ متطابقة",
        ["Login_NoMatch"] = "غير متطابقة — راجع كلمة المرور",
        ["Login_ErrTooShort"] = "كلمة المرور يجب أن تكون 8 أحرف على الأقل.",
        ["Login_ErrNeedConfirm"] = "أعد كتابة نفس كلمة المرور في حقل التأكيد ثم تابع.",
        ["Login_ErrMismatch"] = "كلمتا المرور غير متطابقتين — تأكد أنهما متماثلتان.",
        ["Login_ErrWrongPassword"] = "كلمة المرور غير صحيحة.",
        ["Login_ErrOpenFailed"] = "تعذّر فتح الخزنة: {0}",
        ["Login_ResetConfirmMsg"] = "سيتم حذف كل كلمات المرور وحسابات TOTP نهائياً ولا يمكن استرجاعها.\nهل تريد المتابعة؟",
        ["Login_ResetConfirmTitle"] = "إعادة تعيين الخزنة",

        ["Pass_Title"] = "🔑  كلمات المرور",
        ["Pass_SearchTip"] = "ابحث بالعنوان أو المستخدم أو الموقع",
        ["Pass_Add"] = "➕  إضافة",
        ["Pass_HeaderTitle"] = "العنوان",
        ["Pass_HeaderUsername"] = "اسم المستخدم",
        ["Pass_HeaderWebsite"] = "الموقع",
        ["Pass_HeaderStrength"] = "القوة",
        ["Pass_HeaderUpdated"] = "آخر تعديل",
        ["Pass_Empty"] = "لا توجد كلمات مرور محفوظة بعد — اضغط «إضافة» للبدء",
        ["Pass_Count"] = "المحفوظ: {0} | المعروض: {1}",
        ["Pass_User"] = "المستخدم: {0}",
        ["Pass_Website"] = "الموقع: {0}",
        ["Pass_Category"] = "التصنيف: {0}",
        ["Pass_Notes"] = "ملاحظات: {0}",
        ["Pass_Strength"] = "القوة: {0}",
        ["Pass_BtnUsername"] = "👤 اسم المستخدم",
        ["Pass_BtnPassword"] = "📋 كلمة المرور",
        ["Pass_BtnShow"] = "👁️ إظهار",
        ["Pass_BtnHide"] = "🙈 إخفاء",
        ["Pass_TipEdit"] = "تعديل",
        ["Pass_TipDelete"] = "حذف",
        ["Pass_DeleteConfirm"] = "هل تريد حذف «{0}» نهائياً؟",
        ["Pass_CopiedPassword"] = "تم نسخ كلمة المرور إلى الحافظة.",
        ["Pass_CopiedUsername"] = "تم نسخ اسم المستخدم إلى الحافظة.",

        ["Gen_Title"] = "🎲  مولد كلمات المرور",
        ["Gen_Sub"] = "اضبط الخيارات وسيُتولّد رمز آمن فورياً — يمكنك نسخه أو حفظه مباشرة لموقع محدد.",
        ["Gen_Length"] = "الطول",
        ["Gen_Upper"] = "أحرف كبيرة  (A-Z)",
        ["Gen_Lower"] = "أحرف صغيرة  (a-z)",
        ["Gen_Digits"] = "أرقام  (0-9)",
        ["Gen_Symbols"] = "رموز خاصة  (!@#$%...)",
        ["Gen_ExcludeAmbiguous"] = "استثناء المتشابهة  (I l 1 O 0)",
        ["Gen_GenerateNow"] = "⚡  توليد الآن",
        ["Gen_GeneratedLabel"] = "كلمة المرور المولّدة",
        ["Gen_Strength"] = "القوة",
        ["Gen_Entropy"] = "الانتروبيا التقريبية",
        ["Gen_Bits"] = "{0:0} بت",
        ["Gen_Copy"] = "📋  نسخ",
        ["Gen_Copied"] = "✓  تم النسخ",
        ["Gen_Save"] = "💾  حفظ لموقع محدد",
        ["Gen_New"] = "🎲  كلمة جديدة",
        ["Gen_Saved"] = "✓ تم حفظ كلمة المرور للموقع «{0}» في الخزنة.",

        ["Totp_Title"] = "⏱️  رموز TOTP",
        ["Totp_Sub"] = "رموز المصادقة الثنائية — تتجدد تلقائياً كل 30 ثانية",
        ["Totp_Add"] = "➕  إضافة حساب",
        ["Totp_Empty"] = "لا توجد حسابات TOTP — اضغط «إضافة حساب» واربط أول حساب بكود الموقع.",
        ["Totp_Remaining"] = "يتجدد بعد {0} ثانية",
        ["Totp_InvalidKey"] = "مفتاح غير صالح",
        ["Totp_Copied"] = "تم نسخ رمز «{0}» إلى الحافظة.",
        ["Totp_DeleteConfirm"] = "حذف حساب «{0}» نهائياً؟",

        ["Settings_Title"] = "⚙️  الإعدادات",
        ["Settings_Sub"] = "إدارة الأمان وملف الخزنة",
        ["Settings_ChangeTitle"] = "🔁  تغيير كلمة المرور الرئيسية",
        ["Settings_CurrentPw"] = "كلمة المرور الحالية",
        ["Settings_NewPw"] = "كلمة المرور الجديدة",
        ["Settings_ConfirmPw"] = "تأكيد كلمة المرور الجديدة",
        ["Settings_ChangeBtn"] = "تغيير كلمة المرور",
        ["Settings_ChangeErrCurrent"] = "كلمة المرور الحالية غير صحيحة.",
        ["Settings_ChangeErrTooShort"] = "كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل.",
        ["Settings_ChangeErrMismatch"] = "كلمتا المرور الجديدتان غير متطابقتين.",
        ["Settings_ChangeOk"] = "✓ تم تغيير كلمة المرور الرئيسية بنجاح.",
        ["Settings_VaultTitle"] = "🗄️  ملف الخزنة",
        ["Settings_CopyPath"] = "📋 نسخ المسار",
        ["Settings_ResetDesc"] = "إعادة تعيين الخزنة — يحذف كل كلمات المرور وحسابات TOTP نهائياً",
        ["Settings_ResetBtn"] = "🗑️  إعادة تعيين الخزنة",
        ["Settings_UpdateTitle"] = "🔄  التحديثات",
        ["Settings_CurrentVersion"] = "الإصدار الحالي: {0}",
        ["Settings_CheckBtn"] = "🔄  التحقق من التحديثات",
        ["Settings_DownloadBtn"] = "⬇️  تحميل التحديث",
        ["Settings_UpdateNote"] = "يتم جلب آخر إصدار تلقائياً من مركز التحميلات الرسمي عند كل فتح.",
        ["Settings_Checking"] = "جاري التحقق من التحديثات...",
        ["Settings_ConnFailed"] = "تعذّر الاتصال بالخادم. تأكد من اتصالك بالإنترنت.",
        ["Settings_NewAvailable"] = "يتوفر تحديث جديد: الإصدار {0}",
        ["Settings_UpToDate"] = "أنت على أحدث إصدار ✅",
        ["Settings_AccessTitle"] = "🖥️  الوصول السريع",
        ["Settings_AccessDesc"] = "أنشئ اختصاراً للتطبيق على سطح المكتب ليسهل الوصول إليه.",
        ["Settings_ShortcutBtn"] = "📌  إنشاء اختصار على سطح المكتب",
        ["Settings_ShortcutOk"] = "✓ تم إنشاء الاختصار على سطح المكتب.",
        ["Settings_ShortcutFail"] = "✗ فشل الإنشاء: {0}",
        ["Settings_AboutTitle"] = "ℹ️  حول",
        ["Settings_About"] = "مدير كلمات المرور — الإصدار {0}",
        ["Settings_AboutCrypto"] = "تشفير AES-256-GCM مع مفتاح PBKDF2-SHA256 (600,000 جولة) — جميع البيانات محلية على جهازك.",
        ["Settings_LangTitle"] = "🌐  اللغة",
        ["Settings_LangLabel"] = "لغة الواجهة — تتغير فوراً",
        ["Settings_ResetConfirmMsg"] = "تحذير: سيتم حذف كل كلمات المرور وحسابات TOTP نهائياً ولا يمكن استرجاعها.\nهل أنت متأكد تماماً؟",

        ["Entry_TitleAdd"] = "إضافة كلمة مرور",
        ["Entry_TitleEdit"] = "تعديل كلمة المرور",
        ["Entry_FieldTitle"] = "العنوان / اسم الموقع",
        ["Entry_FieldUsername"] = "اسم المستخدم / البريد الإلكتروني",
        ["Entry_FieldWebsite"] = "الموقع (رابط)",
        ["Entry_FieldCategory"] = "التصنيف",
        ["Entry_FieldPassword"] = "كلمة المرور",
        ["Entry_FieldNotes"] = "ملاحظات",
        ["Entry_Generate"] = "🎲  توليد",
        ["Entry_Save"] = "💾  حفظ",
        ["Entry_Strength"] = "قوة كلمة المرور: {0}   ({1} حرفاً)",
        ["Entry_ErrTitleRequired"] = "العنوان مطلوب.",
        ["Entry_ErrPasswordRequired"] = "كلمة المرور مطلوبة.",

        ["TotpDialog_Title"] = "إضافة حساب TOTP",
        ["TotpDialog_Header"] = "⏱️ إضافة حساب TOTP",
        ["TotpDialog_Secret"] = "المفتاح السري (Secret) أو رابط otpauth:// كاملاً",
        ["TotpDialog_SecretTip"] = "الصق المفتاح من الموقع مثل JBSWY3DPEHPK3PXP أو رابط otpauth الكامل",
        ["TotpDialog_Name"] = "اسم الحساب (مثال: Google، GitHub)",
        ["TotpDialog_Algorithm"] = "الخوارزمية",
        ["TotpDialog_Digits"] = "عدد الخانات",
        ["TotpDialog_Period"] = "الفترة (ثوانٍ)",
        ["TotpDialog_Current"] = "الرمز الحالي:",
        ["TotpDialog_Preview"] = "🔍 معاينة",
        ["TotpDialog_Save"] = "💾  حفظ الحساب",
        ["TotpDialog_ErrSecret"] = "أدخل المفتاح السري أو رابط otpauth.",
        ["TotpDialog_ErrInvalidInput"] = "تعذّر قراءة المفتاح. تأكد من صحته (أحرف A-Z و2-7 فقط).",
        ["TotpDialog_ErrName"] = "أدخل اسم الحساب.",
        ["TotpDialog_ErrKey"] = "المفتاح غير صالح: {0}"
    };

    public static readonly Dictionary<string, string> En = new()
    {
        ["App_Title"] = "Password Manager",
        ["Main_SaveError"] = "Failed to save the vault: {0}",

        ["Sidebar_VaultOpen"] = "Vault is open",
        ["Sidebar_VaultFile"] = "Vault file",
        ["Sidebar_Lock"] = "🔒  Lock vault",
        ["Sidebar_NavPasswords"] = "🔑  Passwords",
        ["Sidebar_NavGenerator"] = "🎲  Password Generator",
        ["Sidebar_NavTotp"] = "⏱️  TOTP Codes",
        ["Sidebar_NavSettings"] = "⚙️  Settings",

        ["Update_BarNew"] = "A new update is available: version {0}",
        ["Update_BarGeneric"] = "A new update is available",
        ["Update_Download"] = "Download update",

        ["Common_Error"] = "Error",
        ["Common_Cancel"] = "Cancel",
        ["Common_Copy"] = "📋 Copy",
        ["Common_Delete"] = "🗑️ Delete",
        ["Common_Copied"] = "✓ Copied",
        ["Common_ConfirmDelete"] = "Confirm deletion",
        ["Common_ClipboardFailed"] = "Could not copy to the clipboard.",
        ["Common_Notice"] = "Notice",

        ["Strength_Empty"] = "Empty",
        ["Strength_Weak"] = "Weak",
        ["Strength_Medium"] = "Medium",
        ["Strength_Strong"] = "Strong",
        ["Strength_VeryStrong"] = "Very strong",

        ["Login_PasswordLabel"] = "Master password (at least 8 characters)",
        ["Login_ShowPassword"] = "Show password",
        ["Login_ConfirmLabel"] = "Re-enter the master password to confirm",
        ["Login_TitleSetup"] = "Create a new vault",
        ["Login_SubSetup"] = "This is your first time using the app — choose a master password that will encrypt all your data. If you forget it, nobody can recover it.",
        ["Login_ActionSetup"] = "Create Vault",
        ["Login_TitleUnlock"] = "Open vault",
        ["Login_SubUnlock"] = "Enter your master password to unlock your data.",
        ["Login_ActionUnlock"] = "Sign in",
        ["Login_ResetLink"] = "Forgot your password? Reset vault",
        ["Login_Strength"] = "Strength: {0}",
        ["Login_Match"] = "✓ Match",
        ["Login_NoMatch"] = "Passwords don't match — check again",
        ["Login_ErrTooShort"] = "The password must be at least 8 characters.",
        ["Login_ErrNeedConfirm"] = "Re-enter the same password in the confirmation field to continue.",
        ["Login_ErrMismatch"] = "The passwords do not match — make sure they are identical.",
        ["Login_ErrWrongPassword"] = "Incorrect password.",
        ["Login_ErrOpenFailed"] = "Failed to open the vault: {0}",
        ["Login_ResetConfirmMsg"] = "All passwords and TOTP accounts will be permanently deleted and cannot be recovered.\nDo you want to continue?",
        ["Login_ResetConfirmTitle"] = "Reset vault",

        ["Pass_Title"] = "🔑  Passwords",
        ["Pass_SearchTip"] = "Search by title, username or website",
        ["Pass_Add"] = "➕  Add",
        ["Pass_HeaderTitle"] = "Title",
        ["Pass_HeaderUsername"] = "Username",
        ["Pass_HeaderWebsite"] = "Website",
        ["Pass_HeaderStrength"] = "Strength",
        ["Pass_HeaderUpdated"] = "Updated",
        ["Pass_Empty"] = "No passwords saved yet — press \"Add\" to start",
        ["Pass_Count"] = "Saved: {0} | Shown: {1}",
        ["Pass_User"] = "Username: {0}",
        ["Pass_Website"] = "Website: {0}",
        ["Pass_Category"] = "Category: {0}",
        ["Pass_Notes"] = "Notes: {0}",
        ["Pass_Strength"] = "Strength: {0}",
        ["Pass_BtnUsername"] = "👤 Username",
        ["Pass_BtnPassword"] = "📋 Password",
        ["Pass_BtnShow"] = "👁️ Show",
        ["Pass_BtnHide"] = "🙈 Hide",
        ["Pass_TipEdit"] = "Edit",
        ["Pass_TipDelete"] = "Delete",
        ["Pass_DeleteConfirm"] = "Delete \"{0}\" permanently?",
        ["Pass_CopiedPassword"] = "Password copied to the clipboard.",
        ["Pass_CopiedUsername"] = "Username copied to the clipboard.",

        ["Gen_Title"] = "🎲  Password Generator",
        ["Gen_Sub"] = "Adjust the options and a secure code is generated instantly — copy it or save it directly for a specific site.",
        ["Gen_Length"] = "Length",
        ["Gen_Upper"] = "Uppercase  (A-Z)",
        ["Gen_Lower"] = "Lowercase  (a-z)",
        ["Gen_Digits"] = "Digits  (0-9)",
        ["Gen_Symbols"] = "Symbols  (!@#$%...)",
        ["Gen_ExcludeAmbiguous"] = "Exclude ambiguous  (I l 1 O 0)",
        ["Gen_GenerateNow"] = "⚡  Generate Now",
        ["Gen_GeneratedLabel"] = "Generated password",
        ["Gen_Strength"] = "Strength",
        ["Gen_Entropy"] = "Approx. entropy",
        ["Gen_Bits"] = "{0:0} bits",
        ["Gen_Copy"] = "📋  Copy",
        ["Gen_Copied"] = "✓  Copied",
        ["Gen_Save"] = "💾  Save for a site",
        ["Gen_New"] = "🎲  New password",
        ["Gen_Saved"] = "✓ Password saved for \"{0}\" in the vault.",

        ["Totp_Title"] = "⏱️  TOTP Codes",
        ["Totp_Sub"] = "Two-factor authentication codes — refresh automatically every 30 seconds",
        ["Totp_Add"] = "➕  Add account",
        ["Totp_Empty"] = "No TOTP accounts yet — press \"Add account\" and link your first account with its site code.",
        ["Totp_Remaining"] = "Refreshes in {0} seconds",
        ["Totp_InvalidKey"] = "Invalid key",
        ["Totp_Copied"] = "Code for \"{0}\" copied to the clipboard.",
        ["Totp_DeleteConfirm"] = "Delete account \"{0}\" permanently?",

        ["Settings_Title"] = "⚙️  Settings",
        ["Settings_Sub"] = "Manage security and the vault file",
        ["Settings_ChangeTitle"] = "🔁  Change master password",
        ["Settings_CurrentPw"] = "Current password",
        ["Settings_NewPw"] = "New password",
        ["Settings_ConfirmPw"] = "Confirm new password",
        ["Settings_ChangeBtn"] = "Change password",
        ["Settings_ChangeErrCurrent"] = "The current password is incorrect.",
        ["Settings_ChangeErrTooShort"] = "The new password must be at least 8 characters.",
        ["Settings_ChangeErrMismatch"] = "The new passwords do not match.",
        ["Settings_ChangeOk"] = "✓ Master password changed successfully.",
        ["Settings_VaultTitle"] = "🗄️  Vault file",
        ["Settings_CopyPath"] = "📋 Copy path",
        ["Settings_ResetDesc"] = "Reset the vault — permanently deletes all passwords and TOTP accounts",
        ["Settings_ResetBtn"] = "🗑️  Reset vault",
        ["Settings_UpdateTitle"] = "🔄  Updates",
        ["Settings_CurrentVersion"] = "Current version: {0}",
        ["Settings_CheckBtn"] = "🔄  Check for updates",
        ["Settings_DownloadBtn"] = "⬇️  Download update",
        ["Settings_UpdateNote"] = "The latest version is fetched automatically from the official download center on every launch.",
        ["Settings_Checking"] = "Checking for updates...",
        ["Settings_ConnFailed"] = "Could not reach the server. Check your internet connection.",
        ["Settings_NewAvailable"] = "A new version is available: {0}",
        ["Settings_UpToDate"] = "You are on the latest version ✅",
        ["Settings_AccessTitle"] = "🖥️  Quick access",
        ["Settings_AccessDesc"] = "Create a desktop shortcut for the app for easy access.",
        ["Settings_ShortcutBtn"] = "📌  Create desktop shortcut",
        ["Settings_ShortcutOk"] = "✓ Desktop shortcut created.",
        ["Settings_ShortcutFail"] = "✗ Creation failed: {0}",
        ["Settings_AboutTitle"] = "ℹ️  About",
        ["Settings_About"] = "Password Manager — version {0}",
        ["Settings_AboutCrypto"] = "AES-256-GCM encryption with a PBKDF2-SHA256 key (600,000 iterations) — all data stays local on your device.",
        ["Settings_LangTitle"] = "🌐  Language",
        ["Settings_LangLabel"] = "Interface language — changes instantly",
        ["Settings_ResetConfirmMsg"] = "Warning: all passwords and TOTP accounts will be permanently deleted and cannot be recovered.\nAre you absolutely sure?",

        ["Entry_TitleAdd"] = "Add password",
        ["Entry_TitleEdit"] = "Edit password",
        ["Entry_FieldTitle"] = "Title / site name",
        ["Entry_FieldUsername"] = "Username / email",
        ["Entry_FieldWebsite"] = "Website (URL)",
        ["Entry_FieldCategory"] = "Category",
        ["Entry_FieldPassword"] = "Password",
        ["Entry_FieldNotes"] = "Notes",
        ["Entry_Generate"] = "🎲  Generate",
        ["Entry_Save"] = "💾  Save",
        ["Entry_Strength"] = "Password strength: {0}   ({1} characters)",
        ["Entry_ErrTitleRequired"] = "A title is required.",
        ["Entry_ErrPasswordRequired"] = "A password is required.",

        ["TotpDialog_Title"] = "Add TOTP account",
        ["TotpDialog_Header"] = "⏱️ Add TOTP account",
        ["TotpDialog_Secret"] = "Secret key or full otpauth:// link",
        ["TotpDialog_SecretTip"] = "Paste the key from the site like JBSWY3DPEHPK3PXP or a full otpauth link",
        ["TotpDialog_Name"] = "Account name (e.g., Google, GitHub)",
        ["TotpDialog_Algorithm"] = "Algorithm",
        ["TotpDialog_Digits"] = "Digits",
        ["TotpDialog_Period"] = "Period (seconds)",
        ["TotpDialog_Current"] = "Current code:",
        ["TotpDialog_Preview"] = "🔍 Preview",
        ["TotpDialog_Save"] = "💾  Save account",
        ["TotpDialog_ErrSecret"] = "Enter the secret key or an otpauth link.",
        ["TotpDialog_ErrInvalidInput"] = "Could not read the key. Make sure it is valid (A-Z and 2-7 only).",
        ["TotpDialog_ErrName"] = "Enter the account name.",
        ["TotpDialog_ErrKey"] = "The key is invalid: {0}"
    };
}
