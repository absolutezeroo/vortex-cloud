using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Storage;

namespace Vortex.Rooms.Grains;

/// <summary>
/// The in-process capabilities the room offers to the object logic running inside its turn.
/// <para>
/// Every member is an explicit interface implementation. That is deliberate: it keeps these off
/// <c>RoomGrain</c>'s own surface (which is already large), and it is what lets the game and freeze
/// subsystems each keep a <c>StartGameAsync</c>/<c>EndGameAsync</c> of their own without colliding.
/// </para>
/// <para>
/// None of these interfaces derives from a grain interface, so Orleans generates no proxy for them
/// and a call through one stays an ordinary method call on this activation.
/// </para>
/// </summary>
public sealed partial class RoomGrain
    : IRoomLookup,
        IRoomMapAccess,
        IRoomGameAccess,
        IRoomFreezeAccess,
        IRoomChestAccess,
        IRoomBanzaiAccess,
        IRoomFurniAccess
{
    private static readonly IReadOnlySet<RoomObjectId> EmptyStack = new HashSet<RoomObjectId>();

    IRoomItem? IRoomLookup.FindItem(RoomObjectId objectId) =>
        _state.ItemsById.TryGetValue(objectId, out IRoomItem? item) ? item : null;

    IRoomAvatar? IRoomLookup.FindAvatar(RoomObjectId objectId) =>
        _state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar) ? avatar : null;

    IRoomAvatar? IRoomLookup.FindAvatarByPlayer(PlayerId playerId) =>
        _state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
        && _state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
            ? avatar
            : null;

    bool IRoomLookup.TryFindItem(RoomObjectId objectId, [NotNullWhen(true)] out IRoomItem? item) =>
        _state.ItemsById.TryGetValue(objectId, out item);

    bool IRoomLookup.TryFindAvatar(
        RoomObjectId objectId,
        [NotNullWhen(true)] out IRoomAvatar? avatar
    ) => _state.AvatarsByObjectId.TryGetValue(objectId, out avatar);

    bool IRoomLookup.TryFindAvatarByPlayer(
        PlayerId playerId,
        [NotNullWhen(true)] out IRoomAvatar? avatar
    )
    {
        if (_state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId))
        {
            return _state.AvatarsByObjectId.TryGetValue(objectId, out avatar);
        }

        avatar = null;

        return false;
    }

    IReadOnlyCollection<IRoomItem> IRoomLookup.Items => _state.ItemsById.Values;

    IReadOnlyCollection<IRoomAvatar> IRoomLookup.Avatars => _state.AvatarsByObjectId.Values;

    int IRoomLookup.AvatarCount => _state.AvatarsByPlayerId.Count;

    int IRoomMapAccess.Width => MapModule.Width;

    int IRoomMapAccess.Height => MapModule.Height;

    int IRoomMapAccess.TileCount => _state.TileFloorStacks.Length;

    int IRoomMapAccess.ToIdx(int x, int y) => MapModule.ToIdx(x, y);

    (int x, int y) IRoomMapAccess.GetTileXY(int idx) => MapModule.GetTileXY(idx);

    int IRoomMapAccess.GetDistanceBetween(int a, int b) => MapModule.GetDistanceBetween(a, b);

    (int dx, int dy) IRoomMapAccess.GetDirectionOffset(Rotation dir) =>
        MapModule.GetDirectionOffset(dir);

    bool IRoomMapAccess.TryGetTileInFront(int index, Rotation direction, out int nextIndex) =>
        MapModule.TryGetTileInFront(index, direction, out nextIndex);

    bool IRoomMapAccess.CanAvatarWalk(
        IRoomAvatar avatar,
        int tileIdx,
        bool isGoal,
        bool isDiagonalCheck
    ) => MapModule.CanAvatarWalk(avatar, tileIdx, isGoal, isDiagonalCheck);

    RoomTileFlags IRoomMapAccess.TileFlagsAt(int tileIndex) =>
        tileIndex >= 0 && tileIndex < _state.TileFlags.Length
            ? _state.TileFlags[tileIndex]
            : RoomTileFlags.None;

    IReadOnlySet<RoomObjectId> IRoomMapAccess.FloorStackAt(int tileIndex) =>
        tileIndex >= 0 && tileIndex < _state.TileFloorStacks.Length
            ? _state.TileFloorStacks[tileIndex]
            : EmptyStack;

    IReadOnlySet<RoomObjectId> IRoomMapAccess.AvatarStackAt(int tileIndex) =>
        tileIndex >= 0 && tileIndex < _state.TileAvatarStacks.Length
            ? _state.TileAvatarStacks[tileIndex]
            : EmptyStack;

    GameTeamColor IRoomGameAccess.GetTeam(PlayerId playerId) => GameSystem.GetTeam(playerId);

    int IRoomGameAccess.GetTeamScore(GameTeamColor team) => GameSystem.GetTeamScore(team);

    IReadOnlyList<PlayerId> IRoomGameAccess.GetPlayersInTeam(GameTeamColor team) =>
        GameSystem.GetPlayersInTeam(team);

    Task IRoomGameAccess.StartGameAsync(CancellationToken ct) => GameSystem.StartGameAsync(ct);

    Task IRoomGameAccess.EndGameAsync(CancellationToken ct) => GameSystem.EndGameAsync(ct);

    Task IRoomGameAccess.JoinTeamAsync(
        PlayerId playerId,
        GameTeamColor team,
        CancellationToken ct
    ) => GameSystem.JoinTeamAsync(playerId, team, ct);

    Task IRoomGameAccess.LeaveTeamAsync(PlayerId playerId, CancellationToken ct) =>
        GameSystem.LeaveTeamAsync(playerId, ct);

    Task<bool> IRoomGameAccess.TryGiveScoreToTeamAsync(
        RoomObjectId box,
        GameTeamColor team,
        int amount,
        int cap,
        CancellationToken ct
    ) => GameSystem.TryGiveScoreToTeamAsync(box, team, amount, cap, ct);

    Task<bool> IRoomGameAccess.TryGiveScoreToPlayerTeamAsync(
        RoomObjectId box,
        PlayerId playerId,
        int amount,
        int cap,
        CancellationToken ct
    ) => GameSystem.TryGiveScoreToPlayerTeamAsync(box, playerId, amount, cap, ct);

    void IRoomGameAccess.LockMovement(PlayerId playerId) => GameChrome.LockMovement(playerId);

    void IRoomGameAccess.UnlockMovement(PlayerId playerId) => GameChrome.UnlockMovement(playerId);

    Task<int> IRoomChestAccess.PayOutChestCreditsAsync(
        int chestId,
        PlayerId playerId,
        int amount,
        bool everything,
        CancellationToken ct
    ) => PayOutWiredChestCreditsAsync(chestId, playerId, amount, everything, ct);

    Task<int> IRoomChestAccess.PayOutChestItemsAsync(
        int chestId,
        PlayerId playerId,
        int count,
        CancellationToken ct
    ) => PayOutWiredChestItemsAsync(chestId, playerId, count, ct);

    Task IRoomFreezeAccess.ThrowBallAsync(
        PlayerId playerId,
        int targetX,
        int targetY,
        CancellationToken ct
    ) => FreezeSystem.ThrowBallAsync(playerId, targetX, targetY, ct);

    Task IRoomFreezeAccess.OnBlockWalkOnAsync(
        PlayerId playerId,
        int x,
        int y,
        CancellationToken ct
    ) => FreezeSystem.OnBlockWalkOnAsync(playerId, x, y, ct);

    Task IRoomFreezeAccess.OnExitWalkOnAsync(PlayerId playerId, CancellationToken ct) =>
        FreezeSystem.OnExitWalkOnAsync(playerId, ct);

    Task IRoomFreezeAccess.OnGateWalkOnAsync(
        PlayerId playerId,
        GameTeamColor team,
        CancellationToken ct
    ) => FreezeSystem.OnGateWalkOnAsync(playerId, team, ct);

    bool IRoomBanzaiAccess.IsRoundRunning => BanzaiSystem.IsRoundRunning;

    Task IRoomBanzaiAccess.OnTileWalkOnAsync(
        PlayerId playerId,
        int tileIdx,
        CancellationToken ct
    ) => BanzaiSystem.OnTileWalkOnAsync(playerId, tileIdx, ct);

    Task IRoomBanzaiAccess.OnGateWalkOnAsync(
        PlayerId playerId,
        GameTeamColor team,
        CancellationToken ct
    ) => BanzaiSystem.OnGateWalkOnAsync(playerId, team, ct);

    Task IRoomBanzaiAccess.OnTeleportWalkOnAsync(
        PlayerId playerId,
        RoomObjectId sourceItemId,
        CancellationToken ct
    ) => BanzaiSystem.OnTeleportWalkOnAsync(playerId, sourceItemId, ct);

    Task<bool> IRoomFurniAccess.ValidateFloorItemPlacementAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot
    ) => FurniModule.ValidateFloorItemPlacementAsync(ctx, itemId, x, y, rot);

    IWiredVariable? IRoomFurniAccess.GetVariableById(WiredVariableId id) =>
        WiredSystem.GetVariableById(id);

    void IRoomFurniAccess.ScheduleFlashRevert(RoomObjectId objectId) =>
        WiredSystem.ScheduleFlashRevert(objectId);

    void IRoomFurniAccess.ResetTimers() => WiredSystem.ResetTimers();

    WiredVariableHash IRoomFurniAccess.AllVariablesHash => _state.AllVariablesHash;

    bool IRoomFurniAccess.TryGetVariableStore(WiredVariableKey key, out IWiredKeyValueStore? store)
    {
        bool found = WiredSystem.TryGetStoreForKey(key, out KeyValueStore? concrete);
        store = concrete;

        return found;
    }

    Task<bool> IRoomFurniAccess.KickUserFromWiredAsync(
        PlayerId targetPlayerId,
        CancellationToken ct
    ) => KickUserFromWiredAsync(targetPlayerId, ct);

    Task<bool> IRoomFurniAccess.MuteUserFromWiredAsync(
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    ) => MuteUserFromWiredAsync(targetPlayerId, durationSeconds, ct);

    Task IRoomFurniAccess.EnsureGuildRosterAsync(int groupId, CancellationToken ct) =>
        EnsureGuildRosterAsync(groupId, ct);

    bool IRoomFurniAccess.IsGuildMember(int groupId, PlayerId player) =>
        IsGuildMember(groupId, player);

    Task IRoomFurniAccess.EnsureWornBadgesAsync(PlayerId player, CancellationToken ct) =>
        EnsureWornBadgesAsync(player, ct);

    bool IRoomFurniAccess.IsWearingBadge(PlayerId player, string badgeCode) =>
        IsWearingBadge(player, badgeCode);

    Task<int> IRoomFurniAccess.ExecuteWiredStacksAtAsync(
        int callerTileIdx,
        IReadOnlyCollection<int> targetFurniIds,
        IWiredSelectionSet inheritedSelection,
        CancellationToken ct
    ) => WiredSystem.ExecuteStacksAtAsync(callerTileIdx, targetFurniIds, inheritedSelection, ct);
}
