using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Inventory.Pets;

public record ConfirmPetBreedingMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
