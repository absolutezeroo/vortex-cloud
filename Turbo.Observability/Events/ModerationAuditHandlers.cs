using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Events.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Events;

/// <summary>Translates moderation domain events into durable audit records (category Moderation).</summary>
public sealed class PlayerKickedFromRoomAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerKickedFromRoomEvent>
{
    public ValueTask HandleAsync(
        PlayerKickedFromRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Moderation,
                Action = "moderation.kick",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
                RoomId = e.RoomId,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PlayerMutedInRoomAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerMutedInRoomEvent>
{
    public ValueTask HandleAsync(PlayerMutedInRoomEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Moderation,
                Action = "moderation.mute",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(new { durationSeconds = e.DurationSeconds }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PlayerBannedInRoomAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerBannedInRoomEvent>
{
    public ValueTask HandleAsync(PlayerBannedInRoomEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Moderation,
                Action = "moderation.ban",
                Severity = AuditSeverity.Warning,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(new { durationSeconds = e.DurationSeconds }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PlayerAlertedAuditHandler(IAuditSink audit) : IEventHandler<PlayerAlertedEvent>
{
    public ValueTask HandleAsync(PlayerAlertedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Moderation,
                Action = "moderation.alert",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
                RoomId = e.RoomId,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ModerationActionDeniedAuditHandler(IAuditSink audit)
    : IEventHandler<ModerationActionDeniedEvent>
{
    public ValueTask HandleAsync(
        ModerationActionDeniedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Moderation,
                Action = "moderation.denied",
                Severity = AuditSeverity.Warning,
                Result = AuditResult.Denied,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(new { action = e.Action }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
