using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Inventory.Pets;

public record ConfirmPetBreedingMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
