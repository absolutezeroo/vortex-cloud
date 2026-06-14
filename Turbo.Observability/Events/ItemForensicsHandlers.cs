using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Events.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Events;

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
                Data = e.Reason is null
                    ? null
                    : JsonSerializer.Serialize(new { reason = e.Reason }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
