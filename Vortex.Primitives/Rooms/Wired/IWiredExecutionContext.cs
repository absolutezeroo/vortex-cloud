using System.Collections.Generic;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
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
        SlideAvatarMoveType moveType,
        WiredWalkMode walkMode = WiredWalkMode.KeepIfCloser
    );
    public Task ProcessUserDirectionAsync(
        IRoomAvatar avatar,
        Rotation bodyRotation,
        Rotation headRotation
    );
    public ActionContext AsActionContext();
    public Task SendComposerToRoomAsync(IComposer composer);

    /// <summary>
    /// Makes the named bot say something. False when no bot in the room answers to that name, which
    /// is what a stack pointing at a bot somebody has since picked up looks like.
    /// </summary>
    public Task<bool> ProcessBotChatAsync(
        string botName,
        string text,
        WiredBotChatType chatType,
        PlayerId? whisperTo
    );
}
