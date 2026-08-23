using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Pets;

/// <summary>Compost a withered monsterplant. One int, the pet's id.</summary>
public record CompostPlantMessage : IMessageEvent
{
    public required int PetId { get; init; }
}
