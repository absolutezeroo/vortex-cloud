using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>Translates social-graph domain events into durable audit records.</summary>
public sealed class FriendRequestAcceptedAuditHandler(IAuditSink audit)
    : IEventHandler<FriendRequestAcceptedEvent>
{
    public ValueTask HandleAsync(
        FriendRequestAcceptedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.friend_request_accepted",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class FriendRemovedAuditHandler(IAuditSink audit) : IEventHandler<FriendRemovedEvent>
{
    public ValueTask HandleAsync(FriendRemovedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.friend_removed",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class UserBlockedAuditHandler(IAuditSink audit) : IEventHandler<UserBlockedEvent>
{
    public ValueTask HandleAsync(UserBlockedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.user_blocked",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class UserUnblockedAuditHandler(IAuditSink audit) : IEventHandler<UserUnblockedEvent>
{
    public ValueTask HandleAsync(UserUnblockedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.user_unblocked",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
            }
        );

        return ValueTask.CompletedTask;
    }
}
