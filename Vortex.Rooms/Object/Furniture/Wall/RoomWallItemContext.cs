using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Object.Furniture.Wall;

public sealed class RoomWallItemContext(RoomGrain roomGrain, IRoomWallItem roomObject)
    : RoomItemContext<IRoomWallItem, IFurnitureWallLogic, IRoomWallItemContext>(
        roomGrain,
        roomObject
    ),
        IRoomWallItemContext { }
