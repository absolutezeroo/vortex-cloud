using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;

namespace Vortex.Rooms.Wired.Engine;

/// <summary>
/// Everything a wired effect is allowed to do to the room.
/// </summary>
/// <remarks>
/// <para>
/// The boxes never see this. They talk to <c>IWiredExecutionContext</c>, which is a different and
/// deliberately narrower contract; this is what the context itself is built on, and it exists so the
/// context can be built on something other than a whole <c>RoomGrain</c>.
/// </para>
/// <para>
/// Nothing here is a general room API. Every member is a capability some wired box actually asks for,
/// and adding one is a decision to be justified in the pull request that needs it — the alternative,
/// handing the engine the grain and letting it help itself, is what this replaces.
/// </para>
/// </remarks>
internal interface IWiredRoomActions
{
    bool InBounds(int tileIdx);

    (int X, int Y) GetTileXY(int tileIdx);

    /// <summary>The floor height of a tile, which is where an avatar rolled onto it lands.</summary>
    Altitude TileHeight(int tileIdx);

    bool MoveFloorItem(IRoomFloorItem item, int tileIdx, Altitude? z, Rotation? rotation);

    bool MoveWallItem(
        IRoomWallItem item,
        int x,
        int y,
        Altitude z,
        Rotation rotation,
        int wallOffset
    );

    /// <summary>Slides an avatar onto a tile. Distinct from a walk: no path, no steps.</summary>
    bool RollAvatar(IRoomAvatar avatar, int tileIdx, Altitude z);

    /// <summary>
    /// Drops an avatar's queued path. A wired move has to do this before relocating them, because
    /// the walk tick steps through that path and would otherwise walk the user straight back
    /// towards where they started.
    /// </summary>
    void CancelWalk(IRoomAvatar avatar);

    Task WalkAvatarToAsync(IRoomAvatar avatar, int x, int y, CancellationToken ct);

    /// <summary>The avatar a player id is standing behind, if they are in the room.</summary>
    bool TryGetAvatar(PlayerId playerId, [NotNullWhen(true)] out IRoomAvatar? avatar);

    Task<BotSnapshot?> FindBotByNameAsync(string botName, CancellationToken ct);

    Task BotSayAsync(
        int botId,
        string text,
        WiredBotChatType chatType,
        PlayerId? whisperTo,
        CancellationToken ct
    );

    Task BotTeleportAsync(int botId, int x, int y, CancellationToken ct);

    Task BotWalkToAsync(int botId, int x, int y, CancellationToken ct);

    Task BotSetFollowTargetAsync(int botId, PlayerId? target, CancellationToken ct);

    Task BotSetFigureAsync(int botId, string figure, CancellationToken ct);

    Task SendComposerToRoomAsync(IComposer composer);

    /// <summary>Puts a hand item in a player's hand. False when they are not there to take it.</summary>
    bool GiveHandItem(PlayerId playerId, int handItemId);
}
