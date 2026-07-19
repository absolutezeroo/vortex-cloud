using Vortex.Primitives.Rooms.Events;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredProcessingContext : IWiredContext
{
    public RoomEvent Event { get; }
    public IWiredStack Stack { get; }
    public IWiredTrigger Trigger { get; }
}
