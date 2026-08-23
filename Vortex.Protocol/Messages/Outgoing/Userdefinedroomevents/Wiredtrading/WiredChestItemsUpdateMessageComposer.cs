using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// What changed in a furniture chest since the screen was filled.
/// </summary>
/// <remarks>
/// Sent after the paged listing, never instead of it: the client applies this to a view it already
/// has. Removals are item ids, additions are whole items.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredChestItemsUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required int ChestId { get; init; }

    [Id(1)]
    public required ImmutableArray<int> RemovedItemIds { get; init; }

    [Id(2)]
    public required ImmutableArray<FurnitureItemSnapshot> AddedItems { get; init; }
}
