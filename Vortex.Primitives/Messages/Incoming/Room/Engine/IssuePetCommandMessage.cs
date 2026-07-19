using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record IssuePetCommandMessage : IMessageEvent
{
    public required int PetId { get; init; }
    public required int CommandId { get; init; }
}
