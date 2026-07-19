using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired;

public sealed class WiredProcessingContext(RoomGrain roomGrain)
    : WiredContext(roomGrain),
        IWiredProcessingContext
{
    public required RoomEvent Event { get; init; }
    public required IWiredStack Stack { get; init; }
    public required IWiredTrigger Trigger { get; init; }
}
