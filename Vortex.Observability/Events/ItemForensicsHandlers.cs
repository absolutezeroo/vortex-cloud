using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>Translates item lifecycle domain events into durable item-forensics records.</summary>
public sealed class ItemCreatedForensicsHandler(IItemForensics forensics)
    : IEventHandler<ItemCreatedEvent>
{
    public ValueTask HandleAsync(ItemCreatedEvent e, EventContext ctx, CancellationToken ct)
    {
        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.ItemId,
                EventType = ItemEventType.Created,
                ActorPlayerId = e.OwnerId,
                ToOwnerId = e.OwnerId,
                Data = e.Data,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ItemPlacedForensicsHandler(IItemForensics forensics)
    : IEventHandler<ItemPlacedEvent>
{
    public ValueTask HandleAsync(ItemPlacedEvent e, EventContext ctx, CancellationToken ct)
    {
        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.ItemId,
                EventType = ItemEventType.Placed,
                ActorPlayerId = e.ActorPlayerId,
                ToOwnerId = e.OwnerId,
                RoomId = e.RoomId,
                Data = e.Data,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ItemMovedForensicsHandler(IItemForensics forensics)
    : IEventHandler<ItemMovedEvent>
{
    public ValueTask HandleAsync(ItemMovedEvent e, EventContext ctx, CancellationToken ct)
    {
        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.ItemId,
                EventType = ItemEventType.Moved,
                ActorPlayerId = e.ActorPlayerId,
                RoomId = e.RoomId,
                Data = e.Data,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ItemTradedForensicsHandler(IItemForensics forensics)
    : IEventHandler<ItemTradedEvent>
{
    public ValueTask HandleAsync(ItemTradedEvent e, EventContext ctx, CancellationToken ct)
    {
        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.ItemId,
                EventType = ItemEventType.Traded,
                ActorPlayerId = e.ActorPlayerId,
                FromOwnerId = e.FromOwnerId,
                ToOwnerId = e.ToOwnerId,
                RoomId = e.RoomId,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ItemPickedUpForensicsHandler(IItemForensics forensics)
    : IEventHandler<ItemPickedUpEvent>
{
    public ValueTask HandleAsync(ItemPickedUpEvent e, EventContext ctx, CancellationToken ct)
    {
        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.ItemId,
                EventType = ItemEventType.PickedUp,
                ActorPlayerId = e.ActorPlayerId,
                FromOwnerId = e.FromOwnerId,
                ToOwnerId = e.ToOwnerId,
                RoomId = e.RoomId,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ItemStaffEditedForensicsHandler(IItemForensics forensics)
    : IEventHandler<ItemStaffEditedEvent>
{
    public ValueTask HandleAsync(ItemStaffEditedEvent e, EventContext ctx, CancellationToken ct)
    {
        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.ItemId,
                EventType = ItemEventType.StaffAction,
                ActorPlayerId = e.ActorPlayerId,
                RoomId = e.RoomId,
                Data = e.Data,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class ItemDeletedForensicsHandler(IItemForensics forensics)
    : IEventHandler<ItemDeletedEvent>
{
    public ValueTask HandleAsync(ItemDeletedEvent e, EventContext ctx, CancellationToken ct)
    {
        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.ItemId,
                EventType = ItemEventType.Deleted,
                ActorPlayerId = e.ActorPlayerId,
                FromOwnerId = e.OwnerId,
                Data = JsonSerializer.Serialize(new { reason = e.Reason.ToString() }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Opening a present is recorded twice on purpose: as an item event on the parcel, which is the row
/// that closes out its life, and (by the audit handler below) as a line on the opener's timeline.
/// The parcel and the contents are different items, so one record cannot serve both questions.
/// </summary>
public sealed class PresentOpenedForensicsHandler(IItemForensics forensics)
    : IEventHandler<PresentOpenedEvent>
{
    public ValueTask HandleAsync(PresentOpenedEvent e, EventContext ctx, CancellationToken ct)
    {
        forensics.Record(
            new ItemForensicEvent
            {
                ItemId = e.ItemId,
                EventType = ItemEventType.Deleted,
                ActorPlayerId = e.ActorPlayerId,
                FromOwnerId = e.ActorPlayerId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(new { reason = "present_opened", e.OfferId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PresentOpenedAuditHandler(IAuditSink audit) : IEventHandler<PresentOpenedEvent>
{
    public ValueTask HandleAsync(PresentOpenedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "item.present_opened",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                RoomId = e.RoomId,
                ItemId = e.ItemId,
                Data = JsonSerializer.Serialize(new { e.OfferId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
