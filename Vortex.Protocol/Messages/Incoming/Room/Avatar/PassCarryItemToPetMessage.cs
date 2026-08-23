using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Avatar;

/// <summary>Gives what the player is holding to a pet standing next to them.</summary>
public record PassCarryItemToPetMessage : IMessageEvent
{
    public int PetId { get; init; }
}
