using Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Mapping;

/// <summary>
/// One walkable surface on a tile: how high it is, what forms it, and what you may do on it.
///
/// A tile has exactly one of these today — the top of the highest item standing on it, or the
/// model's own floor when it is bare. That is the limitation this type exists to lift: with a
/// single surface per tile there is no way to express the floor *under* a raised platform, so an
/// avatar can neither walk beneath one nor be told there is anything down there. Splitting the
/// surface out of the tile is the first half of that; giving a tile several of them is the second.
///
/// Read it through <c>RoomMapModule.GetTopSection()</c> rather than off the flat per-tile arrays.
/// Those arrays still hold the answer, and the accessor still returns exactly one section per tile,
/// so nothing behaves differently — but the callers now ask a question that can have more than one
/// answer, which is what lets the storage change underneath them without touching them again.
/// </summary>
[GenerateSerializer, Immutable]
public readonly record struct RoomTileSection
{
    /// <summary>The flags that describe a *surface* rather than a tile.
    ///
    /// The other members of <see cref="RoomTileFlags" /> — Disabled, Closed, AvatarOccupied,
    /// FurnitureOccupied — are properties of the whole column and stay on the tile. These four come
    /// from whichever item forms the surface, so on a tile with two of them they will differ per
    /// section: the top of a platform can be walkable while the floor beneath it is a seat.</summary>
    public const RoomTileFlags SectionFlags =
        RoomTileFlags.StackBlocked
        | RoomTileFlags.Walkable
        | RoomTileFlags.Sittable
        | RoomTileFlags.Layable;

    /// <summary>The altitude you stand at, absolute — an item contributes <c>Z + stack height</c>,
    /// a bare tile contributes the model's own height.</summary>
    [Id(0)]
    public required Altitude Height { get; init; }

    /// <summary>The item whose top this is, or -1 when the surface is the model's floor.</summary>
    [Id(1)]
    public required RoomObjectId ItemId { get; init; }

    /// <summary>The <see cref="SectionFlags" /> subset, taken from <see cref="ItemId" />.</summary>
    [Id(2)]
    public required RoomTileFlags Flags { get; init; }

    public bool IsWalkable => this.Flags.Has(RoomTileFlags.Walkable);

    public bool IsSittable => this.Flags.Has(RoomTileFlags.Sittable);

    public bool IsLayable => this.Flags.Has(RoomTileFlags.Layable);

    public bool IsStackBlocked => this.Flags.Has(RoomTileFlags.StackBlocked);

    /// <summary>True when nothing stands here and the surface is the model's own floor.</summary>
    public bool IsBareFloor => this.ItemId <= 0;
}
