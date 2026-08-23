using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>The client asking for a chest's contents, after being told to open it.</summary>
public record OpenWiredChestMessage : IMessageEvent
{
    public int ChestId { get; init; }
}
