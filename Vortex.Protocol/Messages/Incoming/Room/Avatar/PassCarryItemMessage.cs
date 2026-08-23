using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Avatar;

/// <summary>Hands what the player is holding to somebody standing next to them.</summary>
public record PassCarryItemMessage : IMessageEvent
{
    /// <summary>The player being handed the item, by their own id rather than their room object's.</summary>
    public int TargetPlayerId { get; init; }
}
