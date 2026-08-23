using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record TogglePetBreedingPermissionMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
