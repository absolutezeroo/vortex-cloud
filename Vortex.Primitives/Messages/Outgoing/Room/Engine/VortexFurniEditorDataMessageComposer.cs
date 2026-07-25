using Orleans;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

/// <summary>
/// The full editable server-side state of one placed furni, answering
/// <c>VortexGetFurniEditorDataMessage</c>. Vortex-specific: no AS3 or Habbo equivalent.
///
/// Also the editor's acknowledgement channel: the same composer is re-sent after a successful edit,
/// so the window redraws from the server's post-edit truth instead of from what the operator typed.
/// A rejected edit sends it too, unchanged, which snaps the window back.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record VortexFurniEditorDataMessageComposer : IComposer
{
    [Id(0)]
    public required RoomObjectId ObjectId { get; init; }

    [Id(1)]
    public required ProductType ProductType { get; init; }

    [Id(2)]
    public required int DefinitionId { get; init; }

    [Id(3)]
    public required int SpriteId { get; init; }

    [Id(4)]
    public required string DefinitionName { get; init; }

    [Id(5)]
    public required int X { get; init; }

    [Id(6)]
    public required int Y { get; init; }

    /// <summary>Altitude in hundredths — see <c>VortexApplyFurniEditMessage.ZHundredths</c>.</summary>
    [Id(7)]
    public required int ZHundredths { get; init; }

    [Id(8)]
    public required Rotation Rotation { get; init; }

    /// <summary>Zero for floor items.</summary>
    [Id(9)]
    public required int WallOffset { get; init; }

    [Id(10)]
    public required string ExtraData { get; init; }

    [Id(11)]
    public required PlayerId OwnerId { get; init; }

    [Id(12)]
    public required string OwnerName { get; init; }

    /// <summary>Empty when the edit succeeded, or when this is an unsolicited read. Otherwise a
    /// short reason code the client shows verbatim — the editor is a staff tool, so a precise
    /// "definition_product_type_mismatch" is more useful than a localized apology.</summary>
    [Id(13)]
    public required string Error { get; init; }
}
