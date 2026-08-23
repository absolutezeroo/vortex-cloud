using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

public record RemoveSaddleFromPetMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
