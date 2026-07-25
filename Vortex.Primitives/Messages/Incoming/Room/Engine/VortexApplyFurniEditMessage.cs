using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

/// <summary>
/// Client applies an edit to one placed furni from the in-client furni editor. Vortex-specific: no
/// AS3 or Habbo equivalent.
///
/// Every field is present on the wire; <see cref="Fields"/> says which ones to honour. Values for
/// unset flags are undefined and must not be read.
/// </summary>
/// <remarks>Serializable because it is passed whole to <c>IRoomGrain.ApplyFurniEditAsync</c>, the same
/// way the wired <c>Update*Message</c> types are — Orleans validates every type in a grain interface
/// signature at silo start, so a plain record here fails the host before it boots.</remarks>
[GenerateSerializer, Immutable]
public record VortexApplyFurniEditMessage : IMessageEvent
{
    [Id(0)]
    public required RoomObjectId ObjectId { get; init; }

    [Id(1)]
    public required FurniEditField Fields { get; init; }

    [Id(2)]
    public required int X { get; init; }

    [Id(3)]
    public required int Y { get; init; }

    /// <summary>Altitude in hundredths, matching <see cref="Altitude.FromInt"/> — the wire has no
    /// double, and the existing floor-item serializer already round-trips altitude at this
    /// precision.</summary>
    [Id(4)]
    public required int ZHundredths { get; init; }

    [Id(5)]
    public required Rotation Rotation { get; init; }

    [Id(6)]
    public required int WallOffset { get; init; }

    [Id(7)]
    public required string ExtraData { get; init; }

    /// <summary>Owner by name, not id: the editor's operator types a username. Resolved server-side;
    /// an unresolvable name fails the whole edit rather than silently keeping the old owner.</summary>
    [Id(8)]
    public required string OwnerName { get; init; }

    [Id(9)]
    public required int DefinitionId { get; init; }
}
