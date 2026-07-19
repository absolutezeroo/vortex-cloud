using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Events.Player;

public abstract record PlayerEvent : RoomEvent
{
    public required PlayerId PlayerId { get; init; }
}
