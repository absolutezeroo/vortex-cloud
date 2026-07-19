using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Pets;

public record CustomizePetWithFurniMessage : IMessageEvent
{
    public required int FurniItemId { get; init; }
    public required int PetId { get; init; }
}
