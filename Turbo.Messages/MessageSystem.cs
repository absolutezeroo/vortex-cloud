using System.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Players;
using Turbo.Primitives.Orleans;
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
    ITurboMetrics metrics,
    ISessionGateway sessionGateway,
    IGrainFactory grainFactory,
    ILogger<MessageSystem> logger
)
{
    private readonly MessageRegistry _registry = registry;
    private readonly ITurboContextAccessor _contextAccessor = contextAccessor;
    private readonly ITurboMetrics _metrics = metrics;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<MessageSystem> _logger = logger;

    public async Task PublishAsync(IMessageEvent env, ISessionContext meta, CancellationToken ct)
    {
        if (_registry is null)
            return;

        var operation = env.GetType().Name;
        var actorId = meta?.SessionKey is null
            ? (long?)null
            : ToPlayerId(_sessionGateway.GetPlayerId(meta.SessionKey));
        var roomId = await ResolveRoomIdAsync(actorId).ConfigureAwait(false);

        using var scope = _contextAccessor.BeginScope(operation, meta?.SessionKey.ToString());

        _metrics.PacketReceived(operation, actorId, roomId);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await _registry.PublishAsync(env, meta, ct).ConfigureAwait(false);
        }
        catch
        {
            _metrics.PacketFailed(operation, actorId, roomId);
            throw;
        }
        finally
        {
            _metrics.PacketCompleted(
                operation,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                actorId,
                roomId
            );
        }
    }

    private static long? ToPlayerId(PlayerId playerId) => playerId.Value > 0 ? playerId.Value : null;

    private async Task<int?> ResolveRoomIdAsync(long? actorId)
    {
        if (!actorId.HasValue)
            return null;

        if (actorId > int.MaxValue)
            return null;

        try
        {
            var playerPresence = _grainFactory.GetPlayerPresenceGrain((int)actorId.Value);
            var room = await playerPresence.GetActiveRoomAsync().ConfigureAwait(false);

            return room.RoomId > 0 ? room.RoomId.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Failed to resolve active room for actor {ActorId} while collecting metrics.",
                actorId
            );

            return null;
        }
    }
}
