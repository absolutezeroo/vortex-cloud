using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Furniture;

public abstract class FurnitureWallVariable(RoomGrain roomGrain)
    : FurnitureVariable<IRoomWallItem>(roomGrain);
