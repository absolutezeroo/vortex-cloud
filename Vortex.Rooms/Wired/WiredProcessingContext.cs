using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Engine;

namespace Vortex.Rooms.Wired;

internal sealed class WiredProcessingContext(IWiredRoomHost host)
    : WiredContext(host),
        IWiredProcessingContext
{
    public required RoomEvent Event { get; init; }
    public required IWiredStack Stack { get; init; }
    public IWiredTrigger? Trigger { get; init; }
}
