using Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Snapshots.Furniture;

/// <summary>
/// One staff furni-editor edit, as the room engine consumes it. Vortex-specific: no AS3 or Habbo
/// equivalent.
///
/// Every field is carried; <see cref="Fields"/> says which ones to honour, and values for unset
/// flags are undefined and must not be read. The owner arrives already resolved here — the editor's
/// operator types a username, which the caller turns into an id before the engine sees it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record FurniEditRequest
{
    [Id(0)]
    public required RoomObjectId ObjectId { get; init; }

    [Id(1)]
    public required FurniEditField Fields { get; init; }

    [Id(2)]
    public required int X { get; init; }

    [Id(3)]
    public required int Y { get; init; }

    /// <summary>Altitude in hundredths, matching <see cref="Altitude.FromInt"/>.</summary>
    [Id(4)]
    public required int ZHundredths { get; init; }

    [Id(5)]
    public required Rotation Rotation { get; init; }

    [Id(6)]
    public required int WallOffset { get; init; }

    [Id(7)]
    public required string ExtraData { get; init; }

    [Id(8)]
    public required int DefinitionId { get; init; }
}
