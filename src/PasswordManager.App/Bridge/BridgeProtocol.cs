using System.IO;
using System.Text;
using System.Text.Json;

namespace PasswordManager.App.Bridge;

/// <summary>
/// ط·ظ„ط¨ طھط¹ط¨ط¦ط© ظ‚ط§ط¯ظ… ظ…ظ† ظ…طھطµظپط­ ط¹ط¨ط± ط§ظ„ط§ظ…طھط¯ط§ط¯.
/// </summary>
internal sealed class FillRequest
{
    public string Type { get; set; } = "fillRequest";
    public string Browser { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// ظ‚ط±ط§ط± ط§ظ„طھط¹ط¨ط¦ط© ط§ظ„ط°ظٹ ظٹط¹ظˆط¯ ط¥ظ„ظ‰ ط§ظ„ظ…طھطµظپط­.
/// </summary>
public sealed class FillDecision
{
    public string Type { get; set; } = "fillResponse";

    /// <summary>none | fill | locked | notrunning</summary>
    public string Decision { get; set; } = "none";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Totp { get; set; } = string.Empty;
    public string TotpAccountName { get; set; } = string.Empty;
    public string EntryTitle { get; set; } = string.Empty;
}

/// <summary>
/// ط¥ط·ط§ط± ط±ط³ط§ط¦ظ„ Native Messaging ظˆظپظ‚ ظ…ظˆط§طµظپط§طھ Chromium: 4 ط¨ط§ظٹطھ ط·ظˆظ„ (little-endian) + JSON UTF-8.
/// </summary>
internal static class BridgeProtocol
{
    public static T? Read<T>(Stream stream, int timeoutMs = 2000) where T : class
    {
        var json = ReadFrame(stream, timeoutMs);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json); }
        catch { return null; }
    }

    public static void Write<T>(Stream stream, T message)
    {
        var json = JsonSerializer.Serialize(message);
        WriteFrame(stream, json);
    }

    public static string? ReadFrame(Stream stream, int timeoutMs)
    {
        var header = ReadExact(stream, 4, timeoutMs);
        if (header is null) return null;

        var length = BitConverter.ToInt32(header, 0);
        if (length <= 0 || length > 16 * 1024 * 1024) return null;

        var body = ReadExact(stream, length, timeoutMs);
        if (body is null) return null;

        return Encoding.UTF8.GetString(body);
    }

    public static void WriteFrame(Stream stream, string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var header = BitConverter.GetBytes(body.Length);
        stream.Write(header, 0, header.Length);
        stream.Write(body, 0, body.Length);
        stream.Flush();
    }

    private static byte[]? ReadExact(Stream stream, int count, int timeoutMs)
    {
        var buffer = new byte[count];
        var read = 0;
        var deadline = Environment.TickCount64 + timeoutMs;

        while (read < count)
        {
            var remaining = (int)(deadline - Environment.TickCount64);
            if (remaining <= 0) break;
            stream.ReadTimeout = remaining;

            int n;
            try { n = stream.Read(buffer, read, count - read); }
            catch (IOException) { break; }
            catch (ObjectDisposedException) { break; }

            if (n <= 0) break;
            read += n;
        }

        return read == count ? buffer : null;
    }
}
