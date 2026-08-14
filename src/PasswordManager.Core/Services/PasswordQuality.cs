namespace PasswordManager.Services;

public static class PasswordQuality
{
    /// <summary>تقييم تقريبي لقوة كلمة المرور حسب الطول وتنوع الأحرف.</summary>
    public static string StrengthLabel(string password)
    {
        if (string.IsNullOrEmpty(password)) return "فارغة";

        var hasLower = password.Any(char.IsLower);
        var hasUpper = password.Any(char.IsUpper);
        var hasDigit = password.Any(char.IsDigit);
        var hasSymbol = password.Any(c => !char.IsLetterOrDigit(c));
        var types = (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSymbol ? 1 : 0);

        if (password.Length >= 16 && types >= 3) return "قوية جداً";
        if (password.Length >= 12 && types >= 3) return "قوية";
        if (password.Length >= 8 && types >= 2) return "متوسطة";
        return "ضعيفة";
    }
}
