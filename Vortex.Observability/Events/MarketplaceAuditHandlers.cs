using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>
/// Marketplace activity as durable audit. The economy ledger already records the credits, so these
/// records exist to answer the half it cannot: which item, and between which two accounts.
/// </summary>
public sealed class MarketplaceOfferListedAuditHandler(IAuditSink audit)
    : IEventHandler<MarketplaceOfferListedEvent>
{
    public ValueTask HandleAsync(
        MarketplaceOfferListedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.marketplace.listed",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.SellerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        e.OfferId,
                        e.DefinitionId,
                        e.Price,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class MarketplaceOfferCancelledAuditHandler(IAuditSink audit)
    : IEventHandler<MarketplaceOfferCancelledEvent>
{
    public ValueTask HandleAsync(
        MarketplaceOfferCancelledEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.marketplace.cancelled",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.SellerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        e.OfferId,
                        e.DefinitionId,
                        e.Price,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// The buyer is the actor and the seller the target, so the pair reads the same way round as a
/// trade does -- and a search on either account surfaces the same line.
/// </summary>
public sealed class MarketplaceOfferBoughtAuditHandler(IAuditSink audit)
    : IEventHandler<MarketplaceOfferBoughtEvent>
{
    public ValueTask HandleAsync(
        MarketplaceOfferBoughtEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.marketplace.bought",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.BuyerId,
                TargetPlayerId = e.SellerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        e.OfferId,
                        e.DefinitionId,
                        e.Price,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class MarketplaceCreditsRedeemedAuditHandler(IAuditSink audit)
    : IEventHandler<MarketplaceCreditsRedeemedEvent>
{
    public ValueTask HandleAsync(
        MarketplaceCreditsRedeemedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.marketplace.credits_redeemed",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.SellerId,
                Data = JsonSerializer.Serialize(new { e.Credits, offers = e.OfferCount }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
