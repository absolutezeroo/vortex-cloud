using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Events.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Events;

/// <summary>Records Habbo Club / VIP purchases and renewals on the economy audit trail.</summary>
public sealed class ClubPurchasedAuditHandler(IAuditSink audit) : IEventHandler<ClubPurchasedEvent>
{
    public ValueTask HandleAsync(ClubPurchasedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = e.IsRenewal ? "economy.hc.renew" : "economy.hc.purchase",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        months = e.Months,
                        isVip = e.IsVip,
                        isRenewal = e.IsRenewal,
                        creditCost = e.CreditCost,
                        totalMonths = e.TotalMonths,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>Records club gift tokens granted to a player on their gift cycle.</summary>
public sealed class ClubGiftTokenGrantedAuditHandler(IAuditSink audit)
    : IEventHandler<ClubGiftTokenGrantedEvent>
{
    public ValueTask HandleAsync(
        ClubGiftTokenGrantedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.hc.gift.token_granted",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(
                    new { tokens = e.Tokens, totalAvailable = e.TotalAvailable }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>Records a club gift claim.</summary>
public sealed class ClubGiftClaimedAuditHandler(IAuditSink audit)
    : IEventHandler<ClubGiftClaimedEvent>
{
    public ValueTask HandleAsync(ClubGiftClaimedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.hc.gift.claimed",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(new { productCode = e.ProductCode }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>Records a Club kickback (payday) reward payout.</summary>
public sealed class ClubPaydayAuditHandler(IAuditSink audit) : IEventHandler<ClubPaydayEvent>
{
    public ValueTask HandleAsync(ClubPaydayEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.hc.payday",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(new { credits = e.Credits }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>Records a Club membership expiry.</summary>
public sealed class ClubExpiredAuditHandler(IAuditSink audit) : IEventHandler<ClubExpiredEvent>
{
    public ValueTask HandleAsync(ClubExpiredEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.hc.expired",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(new { wasVip = e.WasVip }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
