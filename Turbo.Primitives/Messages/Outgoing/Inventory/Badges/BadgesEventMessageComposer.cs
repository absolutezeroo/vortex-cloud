using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.Primitives.Messages.Outgoing.Inventory.Badges;

[GenerateSerializer, Immutable]
public sealed record BadgesEventMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<PlayerBadgeSnapshot> Badges { get; init; }
}
