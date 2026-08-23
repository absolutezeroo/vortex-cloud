using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>The client telling the room it closed a chest's screen.</summary>
public record CloseWiredChestMessage : IMessageEvent
{
    public int ChestId { get; init; }
}
