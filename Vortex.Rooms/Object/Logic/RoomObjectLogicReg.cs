using System;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic;

internal sealed record RoomObjectLogicReg(
    IServiceProvider ServiceProvider,
    Func<IServiceProvider, IRoomObjectContext, IRoomObjectLogic> Factory
)
{
    /// <summary>
    /// Which class registered this. Only read when a second registration collides with it — a
    /// message naming both implementations is the difference between an error somebody can act on
    /// and one they have to go looking for.
    /// </summary>
    public Type? Implementation { get; init; }
};
