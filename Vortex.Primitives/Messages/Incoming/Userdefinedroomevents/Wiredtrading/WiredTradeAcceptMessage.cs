using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

/// <summary>The player accepting a wired trade, or taking that acceptance back.</summary>
/// <remarks>
/// The client sends this twice for one completed trade: false when the player clicks accept, then
/// true from the confirmation dialog. Only the second one may move anything.
/// </remarks>
public record WiredTradeAcceptMessage : IMessageEvent
{
    public required bool Confirm { get; init; }
}
