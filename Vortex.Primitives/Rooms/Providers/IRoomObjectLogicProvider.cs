using System;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Primitives.Rooms.Providers;

public interface IRoomObjectLogicProvider
{
    /// <summary>
    /// Registers a logic under a client logic name.
    /// </summary>
    /// <param name="implementation">
    /// The concrete logic type. The provider reads the room-object family (floor, wall, or neither)
    /// off it, because a single client logic name is shared by both families —
    /// <c>furniture_multistate</c> and <c>furniture_basic</c> each cover thousands of floor
    /// definitions and several hundred wall ones. Keying registrations by name alone let the
    /// last-registered family win, and resolving a wall item against a floor logic cannot even be
    /// constructed: <c>IRoomWallItemContext</c> and <c>IRoomFloorItemContext</c> are disjoint.
    /// </param>
    public IDisposable RegisterLogic(
        string logicType,
        Type implementation,
        IServiceProvider sp,
        Func<IServiceProvider, IRoomObjectContext, IRoomObjectLogic> factory
    );

    public IRoomObjectLogic CreateLogicInstance(string logicType, IRoomObjectContext ctx);
}
