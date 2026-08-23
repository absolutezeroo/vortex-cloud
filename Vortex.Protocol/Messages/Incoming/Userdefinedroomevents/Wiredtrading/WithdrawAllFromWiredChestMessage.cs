using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>Empty the chest into the asking player.</summary>
public record WithdrawAllFromWiredChestMessage : IMessageEvent
{
    public int ChestId { get; init; }
}
