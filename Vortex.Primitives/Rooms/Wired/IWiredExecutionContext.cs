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
    /// <summary>The add-ons of the pile whose actions are running, so an action that says something
    /// can put its text through the text placeholders. The pile itself is not exposed: an action has
    /// no business reaching its siblings.</summary>
    public List<IWiredAddon> Addons { get; }

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

    /// <summary>
    /// Sends the named bot to a tile, either at once or on foot. Walking is a standing order the
    /// room tick works through rather than something that finishes here.
    /// </summary>
    public Task<bool> ProcessBotMovementAsync(string botName, int tileIdx, bool instant);

    /// <summary>Starts the named bot following a player, or stops it when the target is null.</summary>
    public Task<bool> ProcessBotFollowAsync(string botName, PlayerId? target);

    /// <summary>Dresses the named bot in a look captured when the wired was configured.</summary>
    public Task<bool> ProcessBotFigureAsync(string botName, string figure);

    /// <summary>Sends the named bot walking over to a player, for the length of one errand.</summary>
    public Task<bool> ProcessBotWalkToPlayerAsync(string botName, PlayerId target);

    /// <summary>Puts a hand item in a player's hand for the room's configured while.</summary>
    public Task<bool> ProcessGiveHandItemAsync(PlayerId playerId, int handItemId);
}
