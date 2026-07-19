using System;
using System.Collections.Generic;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Providers;

public interface IRoomWiredVariablesProvider
{
    public IDisposable RegisterVariable(
        IServiceProvider sp,
        Func<IServiceProvider, IRoomGrain, IWiredVariable> factory
    );
    public IEnumerable<IWiredVariable> BuildVariablesForRoom(IRoomGrain roomGrain);
}
