using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>Translates authentication domain events into durable audit records.</summary>
public sealed class PlayerLoggedInAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerLoggedInEvent>
{
    public ValueTask HandleAsync(PlayerLoggedInEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Auth,
                Action = "auth.login.success",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                IpHash = e.IpHash,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PlayerLoginFailedAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerLoginFailedEvent>
{
    public ValueTask HandleAsync(PlayerLoginFailedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Auth,
                Action = "auth.login.failed",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Failed,
                IpHash = e.IpHash,
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Account-security changes. All three write one record per character on the account: the audit is
/// searched by character, so a record filed against the account id alone would be invisible from
/// every profile it concerns. An account with no character yet still gets one record, unattributed,
/// rather than none.
/// </summary>
public sealed class AccountPasswordChangedAuditHandler(IAuditSink audit)
    : IEventHandler<AccountPasswordChangedEvent>
{
    public ValueTask HandleAsync(
        AccountPasswordChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        string data = JsonSerializer.Serialize(
            new
            {
                e.AccountId,
                e.StaffReset,
                revokedSessions = e.RevokedSessions,
            }
        );

        foreach (long? actor in Actors(e.PlayerIds))
        {
            audit.Emit(
                new AuditEvent
                {
                    Category = AuditCategory.Security,
                    Action = e.StaffReset ? "security.password.reset" : "security.password.changed",
                    Severity = AuditSeverity.Notice,
                    Result = AuditResult.Success,
                    ActorPlayerId = actor,
                    Data = data,
                }
            );
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>The characters to file against, or a single unattributed record when there are none.</summary>
    internal static IEnumerable<long?> Actors(ImmutableArray<int> playerIds) =>
        playerIds.IsDefaultOrEmpty ? [null] : playerIds.Select(id => (long?)id);
}

public sealed class AccountMfaChangedAuditHandler(IAuditSink audit)
    : IEventHandler<AccountMfaChangedEvent>
{
    public ValueTask HandleAsync(AccountMfaChangedEvent e, EventContext ctx, CancellationToken ct)
    {
        string data = JsonSerializer.Serialize(new { e.AccountId });

        foreach (long? actor in AccountPasswordChangedAuditHandler.Actors(e.PlayerIds))
        {
            audit.Emit(
                new AuditEvent
                {
                    Category = AuditCategory.Security,
                    Action = e.Enabled ? "security.mfa.enabled" : "security.mfa.disabled",
                    Severity = AuditSeverity.Notice,
                    Result = AuditResult.Success,
                    ActorPlayerId = actor,
                    Data = data,
                }
            );
        }

        return ValueTask.CompletedTask;
    }
}

public sealed class PlayerNameChangedAuditHandler(IAuditSink audit)
    : IEventHandler<PlayerNameChangedEvent>
{
    public ValueTask HandleAsync(PlayerNameChangedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Security,
                Action = "security.name.changed",
                // The single most confusing thing that can happen to an investigation: every older
                // record names somebody who, from the directory's point of view, no longer exists.
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(new { from = e.OldName, to = e.NewName }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
