using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record PlacePetMessage : IMessageEvent
{
    public required int PetId { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
}
