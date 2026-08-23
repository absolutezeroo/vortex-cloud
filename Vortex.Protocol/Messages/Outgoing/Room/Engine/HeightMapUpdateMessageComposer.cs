using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record HeightMapUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<(int X, int Y, short Height)> TileHeights { get; init; }
}
