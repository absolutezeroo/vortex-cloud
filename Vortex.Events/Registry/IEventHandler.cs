using Vortex.Pipeline.Registry;
using Vortex.Primitives.Events;

namespace Vortex.Events.Registry;

public interface IEventHandler<in T> : IHandler<T, EventContext>
    where T : IEvent;
