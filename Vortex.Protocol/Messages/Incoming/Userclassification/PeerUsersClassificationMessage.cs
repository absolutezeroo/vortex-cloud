using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userclassification;

/// <summary>
/// <c>:uc hotel &lt;classification&gt;</c> — classify everyone currently online, not just the room.
/// </summary>
public record PeerUsersClassificationMessage : IMessageEvent
{
    /// <summary>The classification the moderator typed; see <c>UserClassifications</c>.</summary>
    public required string Classification { get; init; }
}
