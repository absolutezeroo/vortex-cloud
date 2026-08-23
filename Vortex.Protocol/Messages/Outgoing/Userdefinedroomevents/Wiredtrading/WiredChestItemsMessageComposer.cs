using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// One page of what a furniture chest holds.
/// </summary>
/// <remarks>
/// The client buffers pages and only shows the chest once it receives the last one
/// (<c>fragmentNo == totalFragments - 1</c>), so a chest sent as a single page must still say
/// <c>TotalFragments = 1, FragmentNo = 0</c>. An empty chest is one empty page, not no page at all —
/// otherwise the screen never opens.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredChestItemsMessageComposer : IComposer
{
    [Id(0)]
    public required int ChestId { get; init; }

    [Id(1)]
    public required int TotalFragments { get; init; }

    [Id(2)]
    public required int FragmentNo { get; init; }

    [Id(3)]
    public required ImmutableArray<FurnitureItemSnapshot> Items { get; init; }
}
