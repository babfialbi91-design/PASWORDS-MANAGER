namespace PasswordManager.App.Bridge;

/// <summary>
/// ظ†ظˆط¹ ط§ظ„ط±ط¨ط· ظ…ط¹ ط§ظ„ظ…طھطµظپط­: ط§ظ…طھط¯ط§ط¯ + Native MessagingطŒ ط£ظˆ ط¬ط³ط± ظƒطھط§ط¨ط© ظپظ‚ط·.
/// </summary>
public enum BridgeMethod
{
    /// <summary>ط§ظ…طھط¯ط§ط¯ ظ…ظ„ط­ظ‚ ط¨ظˆط¶ط¹ ط§ظ„ظ…ط·ظˆظ‘ط± + Native Messaging (Chrome/Edge/Brave/Opera/â€¦).</summary>
    Extension,

    /// <summary>ط§ط®طھطµط§ط± ط¹ط§ظ… + ظ„ظˆط­ط© ظƒطھط§ط¨ط© طھط¹ظ…ظ„ ظ…ط¹ ط£ظٹ ظ…طھطµظپط­ (DuckDuckGo/Firefox).</summary>
    Typing
}

/// <summary>
/// ط¨ط·ط§ظ‚ط© طھط¹ط±ظٹظپ ظ…طھطµظپط­ ظپظٹ ط§ظ„ظƒطھط§ظ„ظˆط¬.
/// </summary>
public sealed class BrowserInfo
{
    public required string Id { get; init; }
    public required string NameAr { get; init; }
    public required string NameEn { get; init; }

    /// <summary>ظ…ظپطھط§ط­ ط´ط¹ط§ط± PNG ط¶ظ…ظ† ظ…ظˆط§ط±ط¯ ط§ظ„طھط·ط¨ظٹظ‚ (assets/browsers/).</summary>
    public required string Logo { get; init; }

    /// <summary>ط£ط³ظ…ط§ط، ظ…ظ„ظپط§طھ ط§ظ„طھظ†ظپظٹط° ط§ظ„ظ…ط­طھظ…ظ„ط© (exe).</summary>
    public required string[] Executables { get; init; }

    /// <summary>ظ…ظپطھط§ط­ ط§ظ„ط³ط¬ظ„ ظ„ظ…ط³ط§ط± App PathsطŒ ط£ظˆ null ط¥ظ† ظ„ظ… ظٹظˆط¬ط¯.</summary>
    public string? AppPathsKey { get; init; }

    /// <summary>ظ…ظپطھط§ط­ ط³ط¬ظ„ Native Messaging ط§ظ„ط®ط§طµ ط¨ط§ظ„ظ…طھطµظپط­.</summary>
    public string? NativeHostRegKey { get; init; }

    /// <summary>ظ…ط¹ط±ظ‘ظپ ط§ظ„ظ…طھطµظپط­ ط§ظ„ظ…ط±ط³ظ„ ظپظٹ ط±ط³ط§ط¦ظ„ ط§ظ„ط§ظ…طھط¯ط§ط¯ (ظٹظڈط®ط¨ظژط± ط¨ظ‡ ط§ظ„طھط·ط¨ظٹظ‚ ظپظ‚ط·).</summary>
    public required string BrowserId { get; init; }

    public bool SupportsExtension => NativeHostRegKey is not null;

    /// <summary>ط§ط³ظ… ط§ظ„ظ…طھطµظپط­ ط­ط³ط¨ ط§ظ„ظ„ط؛ط© ط§ظ„ط­ط§ظ„ظٹط©.</summary>
    public string DisplayName => Localization.Instance.IsRtl ? NameAr : NameEn;
}
