using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Pets;

public record PetSelectedMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
