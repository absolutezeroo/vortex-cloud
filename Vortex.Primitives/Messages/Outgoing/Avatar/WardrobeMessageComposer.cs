using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Players;

namespace Vortex.Primitives.Messages.Outgoing.Avatar;

[GenerateSerializer, Immutable]
public sealed record WardrobeMessageComposer : IComposer
{
    [Id(0)]
    public required List<PlayerWardrobeOutfitSnapshot> Outfits { get; init; }
}
