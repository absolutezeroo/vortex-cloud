using Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Mapping;

/// <summary>
/// One item's vertical footprint on a tile: the slab of space it fills, and what standing on its
/// top would let you do.
///
/// This is the whole of what <see cref="RoomTileSectionFinder" /> needs to know about furniture, and
/// deliberately no more — it holds no reference to the item, so the geometry can be reasoned about
/// and tested without a room, a grain or a cluster behind it.
/// </summary>
[GenerateSerializer, Immutable]
public readonly record struct RoomTileOccupant
{
    [Id(0)]
    public required RoomObjectId ItemId { get; init; }

    /// <summary>Where the item rests — <c>Z</c>.</summary>
    [Id(1)]
    public required Altitude Bottom { get; init; }

    /// <summary>Where its surface is — <c>Z + stack height</c>, the same absolute altitude
    /// <c>ComputeTile()</c> compares against the model's floor.</summary>
    [Id(2)]
    public required Altitude Top { get; init; }

    /// <summary>The <see cref="RoomTileSection.SectionFlags" /> subset, from the item's logic.</summary>
    [Id(3)]
    public required RoomTileFlags Flags { get; init; }
}
