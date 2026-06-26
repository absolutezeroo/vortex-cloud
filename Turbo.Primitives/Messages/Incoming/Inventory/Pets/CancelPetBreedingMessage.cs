using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Inventory.Pets;

public record CancelPetBreedingMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
