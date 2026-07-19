using System;
using Vortex.Primitives.Rooms;

namespace Vortex.Players.Grains;

public sealed class PlayerPresenceLiveState
{
    public RoomId ActiveRoomId { get; set; } = -1;
    public RoomId PendingRoomId { get; set; } = -1;
    public bool PendingRoomApproved { get; set; } = false;
    public DateTime ActiveRoomSinceUtc { get; set; } = DateTime.UtcNow;
}
