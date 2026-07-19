using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>Transforms player session/room lifecycle events into durable audit records.</summary>
public sealed class PlayerConnectedAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerConnectedEvent>
{
    public ValueTask HandleAsync(PlayerConnectedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Auth,
                Action = "session.connected",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(new { connectedAtUtc = e.ConnectedAtUtc }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PlayerDisconnectedAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerDisconnectedEvent>
{
    public ValueTask HandleAsync(PlayerDisconnectedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Auth,
                Action = "session.disconnected",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        connectedAtUtc = e.ConnectedAtUtc,
                        disconnectedAtUtc = e.DisconnectedAtUtc,
                        durationSeconds = e.SessionDurationSeconds,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PlayerEnteredRoomAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerEnteredRoomEvent>
{
    public ValueTask HandleAsync(PlayerEnteredRoomEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Room,
                Action = "room.entered",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(new { e.RoomId, e.EnteredAtUtc }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PlayerLeftRoomAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerLeftRoomEvent>
{
    public ValueTask HandleAsync(PlayerLeftRoomEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Room,
                Action = "room.left",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        e.RoomId,
                        e.LeftAtUtc,
                        durationSeconds = e.RoomDurationSeconds,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}
