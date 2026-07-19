using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>Records every wallet movement in the durable economy ledger (intent derived from sign).</summary>
public sealed class CurrencyChangedLedgerHandler(IEconomyLedger ledger)
    : IEventHandler<CurrencyChangedEvent>
{
    public ValueTask HandleAsync(CurrencyChangedEvent e, EventContext ctx, CancellationToken ct)
    {
        if (e.Delta == 0)
        {
            return ValueTask.CompletedTask;
        }

        ledger.Record(
            new EconomyLedgerEvent
            {
                PlayerId = e.PlayerId,
                Currency = e.Currency,
                ActivityPointType = e.ActivityPointType,
                Delta = e.Delta,
                BalanceAfter = e.BalanceAfter,
                Reason = e.Delta < 0 ? EconomyReason.Debit : EconomyReason.Grant,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class CatalogPurchasedAuditHandler(IAuditSink audit)
    : IEventHandler<CatalogPurchasedEvent>
{
    public ValueTask HandleAsync(CatalogPurchasedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.catalog_purchase",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        catalogType = e.CatalogType,
                        offerId = e.OfferId,
                        quantity = e.Quantity,
                        creditCost = e.CreditCost,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class LtdRaffleEnteredAuditHandler(IAuditSink audit)
    : IEventHandler<LtdRaffleEnteredEvent>
{
    public ValueTask HandleAsync(LtdRaffleEnteredEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.ltd.raffle_entry",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(new { seriesId = e.SeriesId, cost = e.Cost }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class LtdRaffleWonAuditHandler(IAuditSink audit) : IEventHandler<LtdRaffleWonEvent>
{
    public ValueTask HandleAsync(LtdRaffleWonEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.ltd.won",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        seriesId = e.SeriesId,
                        serialNumber = e.SerialNumber,
                        furniDefinitionId = e.FurniDefinitionId,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}
