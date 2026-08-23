using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record PopularRoomTagsResultMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<string> Tags { get; init; }
}
