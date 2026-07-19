using System;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Wired.Variables;

internal sealed record WiredVariableReg(
    IServiceProvider ServiceProvider,
    Func<IServiceProvider, IRoomGrain, IWiredVariable> Factory
);
