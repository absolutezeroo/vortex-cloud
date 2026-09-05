using Orleans;
using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Habbicons;

/// <summary>
/// One Habbicon's shop row, in answer to a request for just that one. Same block layout as an entry
/// inside the shop message, so the client caches it into the collection it belongs to.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record HabbiconInfoMessageComposer : IComposer
{
    [Id(0)]
    public required HabbiconShopItemSnapshot Habbicon { get; init; }
}
