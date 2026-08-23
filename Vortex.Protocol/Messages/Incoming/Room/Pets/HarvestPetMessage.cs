using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Pets;

/// <summary>Harvest a full-grown monsterplant. One int, the pet's id.</summary>
public record HarvestPetMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
