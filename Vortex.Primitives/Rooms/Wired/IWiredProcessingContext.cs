using Vortex.Primitives.Rooms.Events;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredProcessingContext : IWiredContext
{
    public RoomEvent Event { get; }
    public IWiredStack Stack { get; }

    /// <summary>The trigger that fired this pile, or null when another pile executed it directly
    /// through the "execute stacks" action, which bypasses triggers by design.</summary>
    public IWiredTrigger? Trigger { get; }
}
