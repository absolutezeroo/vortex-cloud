using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Badges;

[GenerateSerializer, Immutable]
public sealed record BadgesEventMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<PlayerBadgeSnapshot> Badges { get; init; }
}
