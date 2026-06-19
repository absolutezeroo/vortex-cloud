using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Events.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Events;

// Translates group/guild domain events into durable audit records. Auto-discovered at startup
// (closed IEventHandler<T> implementations), so publishing the events is enough to feed metrics +
// audit. Group actions are audited under the Social category with stable "social.group.*" keys.

public sealed class GroupCreatedAuditHandler(IAuditSink audit) : IEventHandler<GroupCreatedEvent>
{
    public ValueTask HandleAsync(GroupCreatedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.created",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        groupId = e.GroupId,
                        groupName = e.GroupName,
                        creditCost = e.CreditCost,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class GroupMemberJoinedAuditHandler(IAuditSink audit)
    : IEventHandler<GroupMemberJoinedEvent>
{
    public ValueTask HandleAsync(GroupMemberJoinedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.member_joined",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class GroupMembershipRequestedAuditHandler(IAuditSink audit)
    : IEventHandler<GroupMembershipRequestedEvent>
{
    public ValueTask HandleAsync(
        GroupMembershipRequestedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.membership_requested",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class GroupMembershipAcceptedAuditHandler(IAuditSink audit)
    : IEventHandler<GroupMembershipAcceptedEvent>
{
    public ValueTask HandleAsync(
        GroupMembershipAcceptedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.membership_accepted",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class GroupMembershipRejectedAuditHandler(IAuditSink audit)
    : IEventHandler<GroupMembershipRejectedEvent>
{
    public ValueTask HandleAsync(
        GroupMembershipRejectedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.membership_rejected",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class GroupMemberKickedAuditHandler(IAuditSink audit)
    : IEventHandler<GroupMemberKickedEvent>
{
    public ValueTask HandleAsync(GroupMemberKickedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.member_kicked",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class GroupMemberRankChangedAuditHandler(IAuditSink audit)
    : IEventHandler<GroupMemberRankChangedEvent>
{
    public ValueTask HandleAsync(
        GroupMemberRankChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.member_rank_changed",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.TargetPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId, isAdmin = e.IsAdmin }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class GroupUpdatedAuditHandler(IAuditSink audit) : IEventHandler<GroupUpdatedEvent>
{
    public ValueTask HandleAsync(GroupUpdatedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.updated",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class GroupDeactivatedAuditHandler(IAuditSink audit)
    : IEventHandler<GroupDeactivatedEvent>
{
    public ValueTask HandleAsync(GroupDeactivatedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.deactivated",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class GroupFavouriteChangedAuditHandler(IAuditSink audit)
    : IEventHandler<GroupFavouriteChangedEvent>
{
    public ValueTask HandleAsync(
        GroupFavouriteChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.favourite_changed",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
