using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Incoming.RoomSettings;

public record UpdateRoomFilterMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
    public bool IsAddingWord { get; init; }
    public string Word { get; init; } = string.Empty;
}
