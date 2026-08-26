using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;

/// <summary>
/// The client reporting that the player clicked another avatar, while a click-user wired box is in
/// the room.
/// </summary>
/// <remarks>
/// Sent alongside the ordinary <c>ClickCharacter</c>, not instead of it — the client suppresses the
/// context menu on this path and then waits to be told whether to open it after all.
/// </remarks>
public record WiredClickUserMessage : IMessageEvent
{
    /// <summary>The clicked avatar's room object id, not a player id.</summary>
    public required int ObjectId { get; init; }
}
