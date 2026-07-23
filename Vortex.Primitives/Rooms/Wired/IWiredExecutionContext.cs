using System.Collections.Generic;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredExecutionContext : IWiredContext
{
    public List<WiredUserMovementSnapshot> UserMoves { get; }
    public List<WiredFloorItemMovementSnapshot> FloorItemMoves { get; }
    public List<WiredWallItemMovementSnapshot> WallItemMoves { get; }
    public List<WiredUserDirectionSnapshot> UserDirections { get; }
    public List<(RoomObjectId, StuffDataSnapshot)> FloorItemStateUpdates { get; }
    public List<(RoomObjectId, string)> WallItemStateUpdates { get; }

    public Task ProcessItemStateUpdateAsync(IRoomItem item, int state);
    public Task ProcessFloorItemMovementAsync(
        IRoomFloorItem floorItem,
        int tileIdx,
        Altitude? z = null,
        Rotation? rotation = null
    );
    public Task ProcessWallItemMovementAsync(
        IRoomWallItem wallItem,
        int x,
        int y,
        Altitude z,
        Rotation rot,
        int wallOffset
    );
    public Task ProcessUserMovementAsync(
        IRoomAvatar avatar,
        int tileIdx,
        SlideAvatarMoveType moveType
    );
    public ActionContext AsActionContext();
    public Task SendComposerToRoomAsync(IComposer composer);
}
