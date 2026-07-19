using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

// Group forum domain events → audit/metrics. Auto-discovered (closed IEventHandler<T>).

public sealed class ForumThreadCreatedAuditHandler(IAuditSink audit)
    : IEventHandler<ForumThreadCreatedEvent>
{
    public ValueTask HandleAsync(ForumThreadCreatedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.forum.thread_created",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId, threadId = e.ThreadId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ForumPostCreatedAuditHandler(IAuditSink audit)
    : IEventHandler<ForumPostCreatedEvent>
{
    public ValueTask HandleAsync(ForumPostCreatedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.forum.post_created",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        groupId = e.GroupId,
                        threadId = e.ThreadId,
                        messageId = e.MessageId,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ForumThreadModeratedAuditHandler(IAuditSink audit)
    : IEventHandler<ForumThreadModeratedEvent>
{
    public ValueTask HandleAsync(
        ForumThreadModeratedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Moderation,
                Action = "social.group.forum.thread_moderated",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        groupId = e.GroupId,
                        threadId = e.ThreadId,
                        state = e.State,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ForumPostModeratedAuditHandler(IAuditSink audit)
    : IEventHandler<ForumPostModeratedEvent>
{
    public ValueTask HandleAsync(ForumPostModeratedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Moderation,
                Action = "social.group.forum.post_moderated",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        groupId = e.GroupId,
                        messageId = e.MessageId,
                        state = e.State,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ForumSettingsUpdatedAuditHandler(IAuditSink audit)
    : IEventHandler<ForumSettingsUpdatedEvent>
{
    public ValueTask HandleAsync(
        ForumSettingsUpdatedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = "social.group.forum.settings_updated",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                Data = JsonSerializer.Serialize(new { groupId = e.GroupId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
