using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Events.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Events;

/// <summary>Writes the player-to-player trade lifecycle (started → completed/cancelled) to the durable
/// business-audit trail under the <c>Item</c> category, so trades surface on the dashboard's audit
/// surfaces the same way catalog purchases and moderation actions do. The per-item ownership moves are
/// recorded separately in the item-forensics stream by <see cref="ItemTradedForensicsHandler"/>.</summary>
public sealed class TradeStartedAuditHandler(IAuditSink audit) : IEventHandler<TradeStartedEvent>
{
    public ValueTask HandleAsync(TradeStartedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "item.trade.started",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerOneId,
                TargetPlayerId = e.PlayerTwoId,
                RoomId = e.RoomId,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class TradeCompletedAuditHandler(IAuditSink audit)
    : IEventHandler<TradeCompletedEvent>
{
    public ValueTask HandleAsync(TradeCompletedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "item.trade.completed",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerOneId,
                TargetPlayerId = e.PlayerTwoId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        playerOne = e.PlayerOneId,
                        playerTwo = e.PlayerTwoId,
                        playerOneItemIds = e.PlayerOneItemIds,
                        playerTwoItemIds = e.PlayerTwoItemIds,
                        playerOneItemCount = e.PlayerOneItemIds.Count,
                        playerTwoItemCount = e.PlayerTwoItemIds.Count,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class TradeCancelledAuditHandler(IAuditSink audit)
    : IEventHandler<TradeCancelledEvent>
{
    public ValueTask HandleAsync(TradeCancelledEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "item.trade.cancelled",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Failed,
                ActorPlayerId = e.PlayerOneId,
                TargetPlayerId = e.PlayerTwoId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(new { reason = e.Reason }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
