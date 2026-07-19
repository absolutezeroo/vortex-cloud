using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Furniture;

public abstract class FurnitureFloorVariable(RoomGrain roomGrain)
    : FurnitureVariable<IRoomFloorItem>(roomGrain);
