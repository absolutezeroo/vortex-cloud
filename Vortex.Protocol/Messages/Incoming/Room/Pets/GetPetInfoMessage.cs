using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Pets;

public record GetPetInfoMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
