using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;

namespace Vortex.Rooms.Object.Logic.Furniture.Wall;

[RoomObjectLogic("default_wall")]
public class FurnitureWallLogic(IStuffDataFactory stuffDataFactory, IRoomWallItemContext ctx)
    : FurnitureLogic<IRoomWallItem, IFurnitureWallLogic, IRoomWallItemContext>(
        stuffDataFactory,
        ctx
    ),
        IFurnitureWallLogic
{
    IRoomWallItemContext IFurnitureWallLogic.Context => Context;
}
