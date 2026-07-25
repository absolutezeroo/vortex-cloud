using System;

namespace Vortex.Primitives.Rooms.Enums;

/// <summary>
/// Which fields of a placed furni an in-client furni-editor request intends to rewrite. The editor
/// always sends every value on the wire (the shape is fixed), so this mask is what distinguishes
/// "set X to 3" from "leave X alone" — without it, opening the editor and touching one field would
/// rewrite all the others with whatever the window happened to be showing.
///
/// Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
[Flags]
public enum FurniEditField
{
    None = 0,

    /// <summary>X and Y. Moved together: a tile is a pair, and the placement validator takes both.</summary>
    Position = 1 << 0,

    Rotation = 1 << 1,

    /// <summary>Z, as a free altitude — not clamped to the stack height under the item.</summary>
    Altitude = 1 << 2,

    /// <summary>Wall items only.</summary>
    WallOffset = 1 << 3,

    ExtraData = 1 << 4,

    Owner = 1 << 5,

    /// <summary>The furniture definition (the item's "base"). Restricted to definitions of the same
    /// <see cref="Furniture.Enums.ProductType"/> — see RoomGrain.Furni.Edit.</summary>
    Definition = 1 << 6,
}
