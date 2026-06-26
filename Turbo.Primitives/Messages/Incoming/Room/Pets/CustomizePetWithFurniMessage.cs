using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Room.Pets;

public record CustomizePetWithFurniMessage : IMessageEvent
{
    public required int FurniItemId { get; init; }
    public required int PetId { get; init; }
}
