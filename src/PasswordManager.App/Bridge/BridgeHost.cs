using System.IO;
using System.IO.Pipes;

namespace PasswordManager.App.Bridge;

/// <summary>
/// وضع الجسر: تُطلقه المتصفحات عبر Native Messaging.
/// يقرأ رسالة واحدة من stdin، يمررها للتطبيق الرئيسي عبر الممر المسماة،
/// ثم يعيد الرد بإطار على stdout ويخرج.
/// </summary>
internal static class BridgeHost
{
    public const int TimeoutMs = 30000;

    public static int Run(string[] args)
    {
        var browser = string.Empty;
        foreach (var arg in args)
        {
            if (arg.StartsWith(BridgeConstants.BrowserArgPrefix, StringComparison.OrdinalIgnoreCase))
                browser = arg[BridgeConstants.BrowserArgPrefix.Length..];
        }

        var response = new FillDecision { Decision = "notrunning" };

        try
        {
            using var stdin = Console.OpenStandardInput();
            var request = BridgeProtocol.Read<FillRequest>(stdin, timeoutMs: 3000);

            if (request is not null)
            {
                request.Browser = browser;
                response = Forward(request) ?? new FillDecision { Decision = "notrunning" };
            }
        }
        catch
        {
            response = new FillDecision { Decision = "notrunning" };
        }

        try
        {
            using var stdout = Console.OpenStandardOutput();
            BridgeProtocol.Write(stdout, response);
            stdout.Flush();
        }
        catch
        {
            // لا يمكن الكتابة — لا شيء يمكن فعله
        }

        return 0;
    }

    private static FillDecision? Forward(FillRequest request)
    {
        using var pipe = new NamedPipeClientStream(".", BridgeConstants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        try
        {
            pipe.Connect(3000);
            if (!pipe.IsConnected) return new FillDecision { Decision = "notrunning" };
        }
        catch
        {
            return new FillDecision { Decision = "notrunning" };
        }

        try
        {
            BridgeProtocol.Write(pipe, request);
            var decision = BridgeProtocol.Read<FillDecision>(pipe, TimeoutMs);
            return decision ?? new FillDecision { Decision = "notrunning" };
        }
        catch
        {
            return new FillDecision { Decision = "notrunning" };
        }
    }
}
