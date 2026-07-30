using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>Records a mystery box key handed to a player. Keys are not furniture, so this row is the
/// only record that one entered circulation -- the source distinguishes a staff grant from a quest
/// reward or an open-failure refund.</summary>
public sealed class MysteryBoxKeyGrantedAuditHandler(IAuditSink audit)
    : IEventHandler<MysteryBoxKeyGrantedEvent>
{
    public ValueTask HandleAsync(
        MysteryBoxKeyGrantedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "mysterybox.key.granted",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                TargetPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(new { color = e.Color, source = e.Source }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>Records a key being spent. Paired with the grant it gives the full key ledger, which is
/// what tells an operator whether keys are being farmed.</summary>
public sealed class MysteryBoxKeyConsumedAuditHandler(IAuditSink audit)
    : IEventHandler<MysteryBoxKeyConsumedEvent>
{
    public ValueTask HandleAsync(
        MysteryBoxKeyConsumedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "mysterybox.key.consumed",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(new { color = e.Color }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Records a box being opened, on both the audit trail and the item's forensic history. A mystery
/// box is the one item destroyed by someone who does not own it, so the deletion is filed against the
/// key holder as actor with the owner as the losing side -- otherwise the box would simply vanish
/// from its owner's history with no explanation.
/// </summary>
public sealed class MysteryBoxOpenedAuditHandler(IAuditSink audit, IItemForensics forensics)
    : IEventHandler<MysteryBoxOpenedEvent>
{
    public ValueTask HandleAsync(MysteryBoxOpenedEvent e, EventContext ctx, CancellationToken ct)
    {
        string data = JsonSerializer.Serialize(
            new
            {
                color = e.Color,
                boxOwnerId = e.BoxOwnerPlayerId,
                keyHolderId = e.KeyHolderPlayerId,
            }
        );

        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "mysterybox.opened",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.KeyHolderPlayerId,
                TargetPlayerId = e.BoxOwnerPlayerId,
                RoomId = e.RoomId,
                ItemId = e.BoxItemId,
                Data = data,
            }
        );

        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.BoxItemId,
                EventType = ItemEventType.Deleted,
                ActorPlayerId = e.KeyHolderPlayerId,
                FromOwnerId = e.BoxOwnerPlayerId,
                RoomId = e.RoomId,
                Data = data,
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>Records what each participant actually won, so a disputed prize can be checked against
/// the pool the server drew from.</summary>
public sealed class MysteryBoxPrizeAwardedAuditHandler(IAuditSink audit)
    : IEventHandler<MysteryBoxPrizeAwardedEvent>
{
    public ValueTask HandleAsync(
        MysteryBoxPrizeAwardedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "mysterybox.prize.awarded",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                TargetPlayerId = e.PlayerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        prizeId = e.PrizeId,
                        color = e.Color,
                        contentType = e.ContentType,
                        classId = e.ClassId,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>Records a mystery trophy exchanged for a real one, and the destruction of the trophy
/// furniture that paid for it.</summary>
public sealed class MysteryTrophyOpenedAuditHandler(IAuditSink audit, IItemForensics forensics)
    : IEventHandler<MysteryTrophyOpenedEvent>
{
    public ValueTask HandleAsync(MysteryTrophyOpenedEvent e, EventContext ctx, CancellationToken ct)
    {
        string data = JsonSerializer.Serialize(
            new { prizeId = e.PrizeId, definitionId = e.PrizeFurnitureDefinitionId }
        );

        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "mysterytrophy.opened",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                RoomId = e.RoomId,
                ItemId = e.TrophyItemId,
                Data = data,
            }
        );

        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.TrophyItemId,
                EventType = ItemEventType.Deleted,
                ActorPlayerId = e.PlayerId,
                FromOwnerId = e.PlayerId,
                RoomId = e.RoomId,
                Data = data,
            }
        );

        return ValueTask.CompletedTask;
    }
}
