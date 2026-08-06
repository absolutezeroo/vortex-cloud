using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record GetRoomChatlogMessage : IMessageEvent
{
    /// <summary>0 for a guest room, 1 for a public space. Sent FIRST on the wire, ahead of
    /// <see cref="RoomId"/> — StartPanelCtrl builds it as <c>_isGuestRoom ? 0 : 1</c>.</summary>
    public int RoomType { get; init; }

    public required int RoomId { get; init; }
}
