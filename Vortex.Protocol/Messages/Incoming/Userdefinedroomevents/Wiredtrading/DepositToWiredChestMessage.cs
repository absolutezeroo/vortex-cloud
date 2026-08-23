using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// The chest screen's deposit button.
/// </summary>
/// <remarks>
/// It carries the chest id and nothing else — no amount, no item — so what the official server does
/// with it cannot be read off the client. Mapped so the message stops being logged as unknown, and
/// so the id is recorded against the right name; the behaviour stays open.
/// </remarks>
public record DepositToWiredChestMessage : IMessageEvent
{
    public required int ChestId { get; init; }
}
