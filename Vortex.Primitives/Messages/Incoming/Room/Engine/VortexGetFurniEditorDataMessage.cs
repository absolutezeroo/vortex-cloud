using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

/// <summary>
/// Client asks for the full editable server-side state of one placed furni, to populate the
/// in-client furni editor. Vortex-specific: no AS3 or Habbo equivalent.
///
/// The editor reads its values from this response rather than from the client's own room objects so
/// that what it shows is the server's truth — the client-side object carries a rendering view of the
/// item (and no owner id, no definition id at all).
/// </summary>
public record VortexGetFurniEditorDataMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
}
