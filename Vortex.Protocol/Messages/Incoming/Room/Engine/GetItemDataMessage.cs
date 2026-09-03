using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

/// <summary>
/// "What does this wall item say?" Sent when a sticky note is opened — the client's room engine
/// raises it from the <c>ROFCAE_STICKIE</c> action rather than from the widget, so it arrives before
/// anything is drawn and the note stays blank until it is answered.
/// </summary>
public record GetItemDataMessage : IMessageEvent
{
    public required int ItemId { get; init; }
}
