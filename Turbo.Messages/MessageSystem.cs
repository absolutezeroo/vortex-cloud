using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;

namespace Turbo.Messages;

/// <summary>
/// Single convergence point for inbound message dispatch (shared by the TCP and WebSocket gateways).
/// This is the natural choke point where a per-packet trace scope is opened (correlation id +
/// Orleans propagation + logging scope) and where pipeline metrics are recorded, without touching
/// any individual handler.
/// </summary>
public sealed class MessageSystem(
    MessageRegistry registry,
    ITurboContextAccessor contextAccessor,
    ITurboMetrics metrics
)
{
    private readonly MessageRegistry _registry = registry;
    private readonly ITurboContextAccessor _contextAccessor = contextAccessor;
    private readonly ITurboMetrics _metrics = metrics;

    public async Task PublishAsync(IMessageEvent env, ISessionContext meta, CancellationToken ct)
    {
        if (_registry is null)
            return;

        var operation = env.GetType().Name;

        using var scope = _contextAccessor.BeginScope(operation, meta?.SessionKey.ToString());

        _metrics.PacketReceived(operation);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await _registry.PublishAsync(env, meta, ct).ConfigureAwait(false);
        }
        catch
        {
            _metrics.PacketFailed(operation);
            throw;
        }
        finally
        {
            _metrics.PacketCompleted(
                operation,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds
            );
        }
    }
}
