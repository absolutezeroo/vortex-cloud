using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record MovePetMessage : IMessageEvent
{
    public required int PetId { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required Rotation Rotation { get; init; }
}
