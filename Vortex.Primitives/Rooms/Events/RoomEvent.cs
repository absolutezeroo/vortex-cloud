using Vortex.Primitives.Action;

namespace Vortex.Primitives.Rooms.Events;

public abstract record RoomEvent
{
    public required RoomId RoomId { get; init; }

    public ActionContext CausedBy { get; init; } = ActionContext.Invalid;
}
