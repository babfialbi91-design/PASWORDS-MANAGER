using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PasswordManager.App.Bridge;

/// <summary>
/// خادم الممر المسماة داخل التطبيق الرئيسي.
/// يستقبل طلبات التعبئة من عمليات الجسر (التي يطلقها المتصفح) ويرد بالقرار.
/// </summary>
internal sealed class BridgeServer : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private Func<FillRequest, FillDecision?>? _resolver;
    private bool _started;

    /// <summary>تثبيت/إزالة دالة معالجة الطلبات (يضبطها التطبيق عندما تُفتح الخزنة).</summary>
    public void SetResolver(Func<FillRequest, FillDecision?>? resolver)
        => _resolver = resolver;

    public void Start()
    {
        if (_started) return;
        _started = true;
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                pipe = new NamedPipeServerStream(
                    BridgeConstants.PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(token);
            }
            catch (OperationCanceledException)
            {
                pipe?.Dispose();
                break;
            }
            catch
            {
                pipe?.Dispose();
                await Task.Delay(500, token);
                continue;
            }

            var client = pipe;
            _ = Task.Run(() => HandleClientAsync(client, token), token);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken token)
    {
        try
        {
            var request = await Task.Run(() => BridgeProtocol.Read<FillRequest>(pipe, 5000), token);
            if (request is null)
            {
                await ReplyAsync(pipe, new FillDecision { Decision = "none" });
                return;
            }

            var resolver = _resolver;
            FillDecision? decision;
            if (resolver is null)
            {
                decision = new FillDecision { Decision = "locked" };
            }
            else
            {
                var dispatcher = Application.Current?.Dispatcher;
                try
                {
                    decision = dispatcher is null || dispatcher.HasShutdownStarted
                        ? resolver(request)
                        : await dispatcher.InvokeAsync(() => resolver(request), DispatcherPriority.Normal).Task;
                }
                catch
                {
                    decision = new FillDecision { Decision = "none" };
                }
            }

            await ReplyAsync(pipe, decision ?? new FillDecision { Decision = "none" });
        }
        catch
        {
            try { await ReplyAsync(pipe, new FillDecision { Decision = "none" }); }
            catch { /* تجاهل */ }
        }
        finally
        {
            pipe.Dispose();
        }
    }

    private static Task ReplyAsync(NamedPipeServerStream pipe, FillDecision decision)
        => Task.Run(() => BridgeProtocol.Write(pipe, decision));

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
