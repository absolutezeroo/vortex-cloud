using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record RemovePetFromFlatMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
