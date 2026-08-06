using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

/// <summary>Dress the mannequin in the requester's current outfit. The figure is not on the wire —
/// the client sends only which mannequin, and the server reads the player's own look.</summary>
public record SetMannequinFigureMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
}
