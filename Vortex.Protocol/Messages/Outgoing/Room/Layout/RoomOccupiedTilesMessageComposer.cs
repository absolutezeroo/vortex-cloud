using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Layout;

/// <summary>
/// The tiles the floor-plan editor must not let you edit, because something is standing on them.
/// The client stores them as its <c>_reservedTiles</c> grid: a reserved tile is drawn in its own
/// colour and <c>setTileHeight</c> refuses to change it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomOccupiedTilesMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<(int X, int Y)> Tiles { get; init; }
}
