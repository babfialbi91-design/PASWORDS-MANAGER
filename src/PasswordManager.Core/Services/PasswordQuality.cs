namespace PasswordManager.Services;

public enum PasswordStrength
{
    Empty,
    Weak,
    Medium,
    Strong,
    VeryStrong
}

public static class PasswordQuality
{
    /// <summary>تقييم تقريبي لقوة كلمة المرور حسب الطول وتنوع الأحرف.</summary>
    public static PasswordStrength Strength(string password)
    {
        if (string.IsNullOrEmpty(password)) return PasswordStrength.Empty;

        var hasLower = password.Any(char.IsLower);
        var hasUpper = password.Any(char.IsUpper);
        var hasDigit = password.Any(char.IsDigit);
        var hasSymbol = password.Any(c => !char.IsLetterOrDigit(c));
        var types = (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSymbol ? 1 : 0);

        if (password.Length >= 16 && types >= 3) return PasswordStrength.VeryStrong;
        if (password.Length >= 12 && types >= 3) return PasswordStrength.Strong;
        if (password.Length >= 8 && types >= 2) return PasswordStrength.Medium;
        return PasswordStrength.Weak;
    }

    /// <summary>ملصق نصي (عربي) لقوة كلمة المرور — يُستخدم في التطبيق النصي.</summary>
    public static string StrengthLabel(string password)
        => Strength(password) switch
        {
            PasswordStrength.Empty => "فارغة",
            PasswordStrength.VeryStrong => "قوية جداً",
            PasswordStrength.Strong => "قوية",
            PasswordStrength.Medium => "متوسطة",
            _ => "ضعيفة"
        };
}
