using System;
using System.Collections.Generic;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.Primitives.Rooms.Providers;

public interface IRoomEventListenerProvider
{
    public IDisposable RegisterListener(
        IServiceProvider sp,
        Func<IServiceProvider, IRoomGrain, IRoomEventListener> factory
    );

    public IEnumerable<IRoomEventListener> BuildListenersForRoom(IRoomGrain roomGrain);
}
