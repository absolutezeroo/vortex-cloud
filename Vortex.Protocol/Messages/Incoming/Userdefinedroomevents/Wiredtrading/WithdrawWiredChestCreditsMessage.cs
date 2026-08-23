using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>Take this many credits out of the chest.</summary>
public record WithdrawWiredChestCreditsMessage : IMessageEvent
{
    public int ChestId { get; init; }

    public int Amount { get; init; }
}
