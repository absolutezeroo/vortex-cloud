using System;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic;

internal sealed record RoomObjectLogicReg(
    IServiceProvider ServiceProvider,
    Func<IServiceProvider, IRoomObjectContext, IRoomObjectLogic> Factory
);
