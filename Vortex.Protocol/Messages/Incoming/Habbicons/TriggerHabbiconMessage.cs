using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Habbicons;

/// <summary>
/// Use a Habbicon in the room the player is standing in, from the selector beside the chat bar.
/// </summary>
public sealed record TriggerHabbiconMessage : IMessageEvent
{
    public required int HabbiconId { get; init; }
}
