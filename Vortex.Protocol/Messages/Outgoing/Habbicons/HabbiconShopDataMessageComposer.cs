using Orleans;
using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Habbicons;

/// <summary>
/// The Habbicon hub: every collection this player may see, each with its entries, its bonus and the
/// player's state on all of them.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record HabbiconShopDataMessageComposer : IComposer
{
    [Id(0)]
    public required HabbiconShopSnapshot Shop { get; init; }
}
