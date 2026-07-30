using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A mystery trophy is opened through its own inscription dialog, which sends
/// <c>OpenMysteryTrophy(objectId, inscription)</c> rather than a plain use -- so this logic exists
/// only to give the furniture the name the client matches on when deciding to offer that dialog.
/// </summary>
[RoomObjectLogic("furniture_mysterytrophy")]
public class FurnitureMysteryTrophyLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx) { }
