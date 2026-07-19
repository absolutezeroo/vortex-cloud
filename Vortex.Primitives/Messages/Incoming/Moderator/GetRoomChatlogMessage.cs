using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record GetRoomChatlogMessage : IMessageEvent
{
    public required int RoomId { get; init; }

    /// <summary>Unconfirmed meaning against the real client (flagged during client-source
    /// research) — parsed to keep the wire read in sync, not currently used for anything.</summary>
    public int Param2 { get; init; }
}
