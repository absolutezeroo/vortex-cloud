using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userclassification;

/// <summary>
/// <c>:uc &lt;classification&gt;</c>, and <c>:anew</c> which sends the literal "new" — classify the
/// people in the room the sender is standing in.
/// </summary>
public record RoomUsersClassificationMessage : IMessageEvent
{
    /// <summary>The classification the moderator typed; see <c>UserClassifications</c>.</summary>
    public required string Classification { get; init; }
}
