using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

public record GetPetCommandsMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
