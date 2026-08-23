using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record ObjectRemoveMultipleMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<long> ObjectIdsToRemove { get; init; }

    [Id(1)]
    public required PlayerId PickerId { get; init; }
}
