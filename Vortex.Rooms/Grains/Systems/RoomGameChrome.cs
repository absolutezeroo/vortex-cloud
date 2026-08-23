using System.Threading.Tasks;
using Vortex.Logging.Extensions;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Protocol.Messages.Outgoing.Room.Action;
using Vortex.Protocol.Messages.Outgoing.Room.Session;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// The one copy of the client-facing plumbing every room game needs: avatar effects (team auras and
/// game effects), the "you are playing a game" client mode, the number bubble over an avatar, and
/// the game-timer reset after an early round end. Before this existed, <see cref="RoomGameSystem"/>
/// and <see cref="RoomFreezeSystem"/> each carried their own copies — the next game would have made
/// a third. <c>RoomAvatarModule.SetAvatarEffectAsync</c> stays deliberately separate: it dedupes on
/// the current effect, which the game paths must not (a re-broadcast is how a round re-asserts an
/// aura the player already wears).
/// <para>All calls run inside the room grain's single-threaded turn.</para>
/// </summary>
public sealed class RoomGameChrome(RoomGrain roomGrain)
{
    private const int NoEffect = 0;

    private readonly RoomGrain _roomGrain = roomGrain;

    /// <summary>Persists the effect on the avatar (so a late joiner's snapshot re-syncs it) and
    /// broadcasts it to the room. Unconditional — game paths never dedupe.</summary>
    public Task BroadcastEffectAsync(PlayerId playerId, int effectId)
    {
        if (!_roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId))
        {
            return Task.CompletedTask;
        }

        if (_roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar))
        {
            avatar.SetEffect(effectId);
        }

        return _roomGrain.SendComposerToRoomAsync(
            new AvatarEffectMessageComposer
            {
                UserId = objectId,
                EffectId = effectId,
                DelayMilliseconds = 0,
            }
        );
    }

    /// <summary>Applies the team aura from the given effect set — or clears the effect when the
    /// player is on no team.</summary>
    public Task BroadcastTeamAuraAsync(PlayerId playerId, GameAuraSet aura, GameTeamColor team) =>
        BroadcastEffectAsync(
            playerId,
            GameTeamState.IsRealTeam(team) ? (int)aura + (int)team : NoEffect
        );

    public Task ClearEffectAsync(PlayerId playerId) => BroadcastEffectAsync(playerId, NoEffect);

    /// <summary>Flips the client's "game mode" (the generic YouArePlayingGame message — no game has
    /// a bespoke HUD protocol in this client).</summary>
    public Task SetPlayingModeAsync(PlayerId playerId, bool isPlaying) =>
        _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(playerId)
            .SendComposerAsync(new YouArePlayingGameMessageComposer { IsPlaying = isPlaying });

    /// <summary>The leave-path variant of <see cref="SetPlayingModeAsync"/>, and the ONLY sanctioned
    /// way to clear game mode from a player-left hook. It is deliberately not awaited: player-left
    /// runs while the leaver's own presence grain is mid-call into this room — including from
    /// OnDeactivateAsync, where the activation no longer dispatches incoming requests — so awaiting
    /// a call back into it would hang the room's turn (every player in it) until the 30s Orleans
    /// call timeout.</summary>
    public void SetPlayingModeAndForget(PlayerId playerId, bool isPlaying) =>
        SetPlayingModeAsync(playerId, isPlaying)
            .LogAndForget(
                _roomGrain._logger,
                "Failed to clear game mode for player {PlayerId} leaving room {RoomId}",
                playerId,
                _roomGrain.RoomId
            );

    /// <summary>Shows <paramref name="value"/> as a number bubble over the player's avatar (0 clears
    /// it) — Freeze uses it for remaining lives.</summary>
    public Task BroadcastPlayerValueAsync(PlayerId playerId, int value)
    {
        if (!_roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId))
        {
            return Task.CompletedTask;
        }

        return _roomGrain.SendComposerToRoomAsync(
            new GamePlayerValueMessageComposer { UserId = objectId, Value = value }
        );
    }

    /// <summary>Roots the player where they stand: locks new walks and cancels the in-flight one.
    /// The wired freeze-user box and a Freeze hit both come through here so "frozen" means one
    /// thing. The lock lives on the avatar, so it dies with the room presence and cannot leak.</summary>
    public void LockMovement(PlayerId playerId)
    {
        if (!TryGetAvatar(playerId, out IRoomAvatar? avatar) || avatar is null)
        {
            return;
        }

        avatar.SetMovementLocked(true);
        _roomGrain.AvatarModule.CancelWalk(avatar);
    }

    /// <summary>Releases a movement lock (wired unfreeze-user, a Freeze thaw, round end).</summary>
    public void UnlockMovement(PlayerId playerId)
    {
        if (TryGetAvatar(playerId, out IRoomAvatar? avatar) && avatar is not null)
        {
            avatar.SetMovementLocked(false);
        }
    }

    private bool TryGetAvatar(PlayerId playerId, out IRoomAvatar? avatar)
    {
        avatar = null;

        return _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out avatar);
    }

    /// <summary>Resets every game-timer furni's countdown and <c>gameActive</c> flag. Needed by any
    /// game that ends the round early (on a normal expiry the furni resets itself).</summary>
    public void ResetGameTimers()
    {
        foreach (
            FurnitureGameTimerLogic timer in _roomGrain._state.ItemIndex.LogicsOf<FurnitureGameTimerLogic>()
        )
        {
            ((IWiredCounter)timer).ResetClock();
        }
    }
}
