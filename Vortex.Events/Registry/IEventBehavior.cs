using Vortex.Pipeline.Registry;
using Vortex.Primitives.Events;

namespace Vortex.Events.Registry;

public interface IEventBehavior<in T> : IBehavior<T, EventContext>
    where T : IEvent;
