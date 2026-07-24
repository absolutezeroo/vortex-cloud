using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Grains.Systems.Freeze;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Grain-side controller for the room's Freeze minigame — the thin, IO-owning wrapper around the pure
/// <see cref="RoomFreezeGame"/> (mirroring how <see cref="RoomGameSystem"/> wraps
/// <see cref="GameTeamState"/>). It turns the game's state changes into avatar-effect broadcasts, gate
/// counter updates, teleports and score, drives the snowball throw pipeline and the 1s freeze tick, and
/// resolves the live balance from server config each round. All calls run inside the room grain's
/// single-threaded turn, so no locking.
/// <para>
/// The Freeze game is deliberately separate from <see cref="RoomGameSystem"/> (the wired Battle-Banzai
/// style team game): players join via physical gates, carry rich per-player state (lives, ammo,
/// power-ups) and wear the Freeze effect set, none of which the generic team system models.
/// </para>
/// </summary>
public sealed class RoomFreezeSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;
    private readonly RoomFreezeGame _game = new();

    // A snowball's flight: the blast lands BlastDelayMs after the throw, then the ripple resets
    // ResetDelayMs after that. Kept as time-ordered queues drained each room tick.
    private readonly PriorityQueue<FreezeBlast, long> _blasts = new();
    private readonly PriorityQueue<List<int>, long> _resets = new();

    private long _currentTickMs;
    private long _nextPlayerTickMs;

    // Armed at kick-off only when two or more teams have players, so the round can end early the moment
    // one team is wiped out — while a solo/one-team game still runs to the timer instead of instantly
    // "winning".
    private bool _endEarlyArmed;

    private readonly record struct FreezeBlast(
        int X,
        int Y,
        int Radius,
        bool Diagonal,
        PlayerId Thrower
    );

    public bool IsRunning => _game.IsRunning;

    public GameTeamColor GetTeam(PlayerId playerId) => _game.GetTeam(playerId);

    // ---- lifecycle ---------------------------------------------------------

    public async Task OnGateWalkOnAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct)
    {
        FreezeGateResult result = _game.ToggleGate(playerId, team);

        if (result == FreezeGateResult.None)
        {
            return;
        }

        await BroadcastEffectAsync(
            playerId,
            result == FreezeGateResult.Joined
                ? FreezeConstants.TeamEffectBase + (int)team
                : FreezeConstants.NoEffect
        );

        await RefreshGateCountersAsync();
    }

    public async Task StartGameAsync(CancellationToken ct)
    {
        _game.Settings = await FreezeConfig.ResolveAsync(
            _roomGrain._grainFactory.GetServerConfigGrain()
        );

        if (!_game.Start())
        {
            return;
        }

        _blasts.Clear();
        _resets.Clear();
        _nextPlayerTickMs = 0;
        _endEarlyArmed = _game.LivingTeamCount() >= 2;

        await ResetBlocksAsync();

        foreach ((PlayerId playerId, FreezePlayerState player) in _game.Players)
        {
            await SetPlayingModeAsync(playerId, true);
            await BroadcastEffectAsync(playerId, player.CurrentEffect());
            await BroadcastPlayerValueAsync(playerId, player.Lives);
        }

        await RefreshGateCountersAsync();
        await RefreshScoreboardsAsync();
    }

    public async Task<GameTeamColor> EndGameAsync(CancellationToken ct)
    {
        GameTeamColor winner = _game.Stop();

        _endEarlyArmed = false;
        _blasts.Clear();
        _resets.Clear();

        foreach ((PlayerId playerId, _) in _game.Players)
        {
            await BroadcastEffectAsync(playerId, FreezeConstants.NoEffect);
            await BroadcastPlayerValueAsync(playerId, 0);
            await SetPlayingModeAsync(playerId, false);
        }

        await RefreshGateCountersAsync();
        await RefreshScoreboardsAsync();

        return winner;
    }

    public async Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct)
    {
        if (_game.Remove(playerId) is not null)
        {
            await RefreshGateCountersAsync();
        }
    }

    /// <summary>A player walked onto an exit tile: they leave the game (forfeit), and their effect and the
    /// gate counters are cleared. No-op for anyone not in the game.</summary>
    public async Task OnExitWalkOnAsync(PlayerId playerId, CancellationToken ct)
    {
        if (_game.GetPlayer(playerId) is null)
        {
            return;
        }

        await BroadcastEffectAsync(playerId, FreezeConstants.NoEffect);
        await BroadcastPlayerValueAsync(playerId, 0);
        await SetPlayingModeAsync(playerId, false);
        _game.Remove(playerId);
        await RefreshGateCountersAsync();
    }

    /// <summary>Room-tick entry: lands due snowball blasts, resets finished ripples and runs the 1s
    /// freeze/shield countdown. Cheap when no game is running.</summary>
    public async Task ProcessAsync(long now, CancellationToken ct)
    {
        _currentTickMs = now;

        if (!_game.IsRunning)
        {
            return;
        }

        while (_blasts.TryPeek(out FreezeBlast blast, out long blastDue) && blastDue <= now)
        {
            _blasts.Dequeue();
            await HandleBlastAsync(blast, now, ct);
        }

        while (_resets.TryPeek(out List<int>? tiles, out long resetDue) && resetDue <= now)
        {
            _resets.Dequeue();
            await ResetTilesAsync(tiles);
        }

        // A round that started with two+ teams ends the moment only one (or none) is left standing.
        if (_endEarlyArmed && _game.LivingTeamCount() <= 1)
        {
            await EndGameAsync(ct);

            return;
        }

        if (_nextPlayerTickMs == 0)
        {
            _nextPlayerTickMs = now + FreezeConstants.FreezeTickMs;
        }

        if (now >= _nextPlayerTickMs)
        {
            await TickPlayersAsync(ct);
            _nextPlayerTickMs = now + FreezeConstants.FreezeTickMs;
        }
    }

    private async Task TickPlayersAsync(CancellationToken ct)
    {
        foreach ((PlayerId playerId, FreezePlayerState player) in _game.Players.ToList())
        {
            if (player.Tick())
            {
                await BroadcastEffectAsync(playerId, player.CurrentEffect());
            }
        }
    }

    /// <summary>A player double-clicked a freeze tile: launch a snowball at it if the rules allow (game
    /// running, has a snowball, target is an idle arena tile adjacent to the thrower).</summary>
    public async Task ThrowBallAsync(
        PlayerId playerId,
        int targetX,
        int targetY,
        CancellationToken ct
    )
    {
        if (!_game.IsRunning)
        {
            return;
        }

        FreezePlayerState? player = _game.GetPlayer(playerId);

        if (player is null || !player.CanThrow || !TryGetAvatar(playerId, out IRoomAvatar? thrower))
        {
            return;
        }

        // Must be the thrower's own tile or one adjacent (Chebyshev <= 1).
        if (Math.Max(Math.Abs(thrower!.X - targetX), Math.Abs(thrower.Y - targetY)) > 1)
        {
            return;
        }

        int targetIdx = _roomGrain.MapModule.ToIdx(targetX, targetY);
        FurnitureFreezeTileLogic? tile = FindFreezeTile(targetIdx);

        // Only onto an idle arena tile — never mid-animation.
        if (tile is null || tile.GetState() != FreezeConstants.TileIdle)
        {
            return;
        }

        player.SpendSnowball();

        // Turn to face the tile being thrown at (unless it is the thrower's own tile).
        if (targetX != thrower!.X || targetY != thrower.Y)
        {
            Rotation facing = RotationExtensions.FromPoints(thrower.X, thrower.Y, targetX, targetY);
            thrower.SetHeadRotation(facing);
            thrower.SetBodyRotation(facing);

            await _roomGrain.SendComposerToRoomAsync(
                new UserUpdateMessageComposer { Avatars = [thrower.GetSnapshot()] }
            );
        }

        int radius = player.TakeThrowRadius();
        bool diagonal = player.NextDiagonal;
        player.NextDiagonal = false;

        // Rise animation now (the ball rises to height radius + 1); the blast lands BlastDelayMs later.
        await tile.SetStateAsync((radius + 1) * FreezeConstants.StateWireScale);

        _blasts.Enqueue(
            new FreezeBlast(targetX, targetY, radius, diagonal, playerId),
            _currentTickMs + FreezeConstants.BlastDelayMs
        );
    }

    private async Task HandleBlastAsync(FreezeBlast blast, long now, CancellationToken ct)
    {
        FreezePlayerState? thrower = _game.GetPlayer(blast.Thrower);
        List<int> animated = [];

        foreach (
            (int x, int y) in FreezeBlastGeometry.AffectedTiles(
                blast.X,
                blast.Y,
                blast.Radius,
                blast.Diagonal
            )
        )
        {
            int idx = _roomGrain.MapModule.ToIdx(x, y);

            if (idx < 0)
            {
                continue;
            }

            // Arena tiles flash and freeze whoever stands on them...
            if (FindFreezeTile(idx) is FurnitureFreezeTileLogic tile)
            {
                await tile.SetStateAsync(
                    FreezeConstants.TileBlast * FreezeConstants.StateWireScale
                );
                animated.Add(idx);

                await FreezeOccupantsAsync(idx, thrower, blast.Thrower, ct);
            }

            // ...and an ice block on the tile is shattered, maybe dropping a power-up.
            await DestroyBlockAsync(idx, thrower, ct);
        }

        if (animated.Count > 0)
        {
            _resets.Enqueue(
                animated,
                now + FreezeConstants.ResetDelayMs - FreezeConstants.BlastDelayMs
            );
        }

        await RefreshScoreboardsAsync();
    }

    private async Task FreezeOccupantsAsync(
        int tileIdx,
        FreezePlayerState? thrower,
        PlayerId throwerId,
        CancellationToken ct
    )
    {
        if (tileIdx < 0 || tileIdx >= _roomGrain._state.TileAvatarStacks.Length)
        {
            return;
        }

        foreach (RoomObjectId avatarId in _roomGrain._state.TileAvatarStacks[tileIdx].ToList())
        {
            if (
                !_roomGrain._state.AvatarsByObjectId.TryGetValue(avatarId, out IRoomAvatar? avatar)
                || avatar is not IRoomPlayer roomPlayer
            )
            {
                continue;
            }

            FreezePlayerState? victim = _game.GetPlayer(roomPlayer.PlayerId);

            if (victim is null || !victim.CanBeFrozen)
            {
                continue;
            }

            // Freezing an enemy scores; catching your own team (or yourself) is a friendly-fire penalty.
            if (thrower is not null)
            {
                int points =
                    victim.Team == thrower.Team
                        ? -_game.Settings.FreezePlayerPoints
                        : _game.Settings.FreezePlayerPoints;

                _game.AddTeamScore(thrower.Team, points);
            }

            bool died = victim.Freeze();

            if (died)
            {
                await EliminateAsync(victim, avatar, ct);
            }
            else
            {
                await BroadcastEffectAsync(victim.PlayerId, victim.CurrentEffect());
                await BroadcastPlayerValueAsync(victim.PlayerId, victim.Lives);
            }
        }
    }

    private async Task EliminateAsync(
        FreezePlayerState victim,
        IRoomAvatar avatar,
        CancellationToken ct
    )
    {
        await BroadcastEffectAsync(victim.PlayerId, FreezeConstants.NoEffect);
        await BroadcastPlayerValueAsync(victim.PlayerId, 0);
        await SetPlayingModeAsync(victim.PlayerId, false);

        if (TryFindRandomExitTile(out int exitIdx))
        {
            _roomGrain.MapModule.RollAvatar(
                avatar,
                exitIdx,
                _roomGrain._state.TileHeights[exitIdx]
            );

            await _roomGrain.SendComposerToRoomAsync(
                new UserUpdateMessageComposer { Avatars = [avatar.GetSnapshot()] }
            );
        }

        _game.Remove(victim.PlayerId);

        await RefreshGateCountersAsync();
    }

    private async Task ResetTilesAsync(List<int> tileIndices)
    {
        foreach (int idx in tileIndices)
        {
            if (FindFreezeTile(idx) is FurnitureFreezeTileLogic tile)
            {
                await tile.SetStateAsync(FreezeConstants.TileIdle);
            }
        }
    }

    /// <summary>Shatters an intact ice block caught in a blast: it rolls the power-up chance and either
    /// reveals a random power-up (states 2..7) or breaks empty (state 1), scoring the thrower's team for
    /// the kill. Already-broken blocks are inert.</summary>
    private async Task DestroyBlockAsync(int idx, FreezePlayerState? thrower, CancellationToken ct)
    {
        if (FindFreezeBlock(idx) is not FurnitureFreezeBlockLogic block)
        {
            return;
        }

        if (block.GetState() != FreezeConstants.BlockIntact)
        {
            return;
        }

        int revealState = FreezeConstants.BlockEmpty;

        if (Random.Shared.Next(100) < _game.Settings.PowerUpChancePercent)
        {
            FreezePowerUp powerUp = FreezePowerUps.Pick(
                Random.Shared.Next(FreezePowerUps.Pickable.Length)
            );
            revealState = FreezePowerUps.RevealState(powerUp);
        }

        await block.SetStateAsync(revealState * FreezeConstants.StateWireScale);

        if (thrower is not null)
        {
            _game.AddTeamScore(thrower.Team, _game.Settings.DestroyBlockPoints);
        }
    }

    /// <summary>A player stepped onto a broken block: if it is showing an uncollected power-up, apply it,
    /// score the pick-up and fade the icon out (the client plays the collect transition).</summary>
    public async Task OnBlockWalkOnAsync(PlayerId playerId, int x, int y, CancellationToken ct)
    {
        if (!_game.IsRunning)
        {
            return;
        }

        FreezePlayerState? player = _game.GetPlayer(playerId);

        if (player is null || player.Dead)
        {
            return;
        }

        int idx = _roomGrain.MapModule.ToIdx(x, y);

        if (idx < 0 || FindFreezeBlock(idx) is not FurnitureFreezeBlockLogic block)
        {
            return;
        }

        int state = block.GetState() / FreezeConstants.StateWireScale;
        FreezePowerUp powerUp = FreezePowerUps.FromRevealState(state);

        if (powerUp == FreezePowerUp.None)
        {
            return;
        }

        FreezePowerUps.Apply(powerUp, player);
        _game.AddTeamScore(player.Team, _game.Settings.PowerUpPoints);

        await block.SetStateAsync(
            (state + FreezeConstants.BlockCollectedOffset) * FreezeConstants.StateWireScale
        );

        // A shield pick-up changes the effect the player wears; an extra life changes the lives bubble.
        await BroadcastEffectAsync(playerId, player.CurrentEffect());

        if (powerUp == FreezePowerUp.ExtraLife)
        {
            await BroadcastPlayerValueAsync(playerId, player.Lives);
        }

        await RefreshScoreboardsAsync();
    }

    /// <summary>Restores every ice block in the room to intact for a fresh round.</summary>
    private async Task ResetBlocksAsync()
    {
        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (
                item.Logic is FurnitureFreezeBlockLogic block
                && block.GetState() != FreezeConstants.BlockIntact
            )
            {
                await block.SetStateAsync(FreezeConstants.BlockIntact);
            }
        }
    }

    private bool TryGetAvatar(PlayerId playerId, out IRoomAvatar? avatar)
    {
        avatar = null;

        return _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out avatar);
    }

    private FurnitureFreezeTileLogic? FindFreezeTile(int tileIdx)
    {
        if (tileIdx < 0 || tileIdx >= _roomGrain._state.TileFloorStacks.Length)
        {
            return null;
        }

        foreach (RoomObjectId id in _roomGrain._state.TileFloorStacks[tileIdx])
        {
            if (
                _roomGrain._state.ItemsById.TryGetValue(id, out IRoomItem? item)
                && item.Logic is FurnitureFreezeTileLogic tile
            )
            {
                return tile;
            }
        }

        return null;
    }

    private FurnitureFreezeBlockLogic? FindFreezeBlock(int tileIdx)
    {
        if (tileIdx < 0 || tileIdx >= _roomGrain._state.TileFloorStacks.Length)
        {
            return null;
        }

        foreach (RoomObjectId id in _roomGrain._state.TileFloorStacks[tileIdx])
        {
            if (
                _roomGrain._state.ItemsById.TryGetValue(id, out IRoomItem? item)
                && item.Logic is FurnitureFreezeBlockLogic block
            )
            {
                return block;
            }
        }

        return null;
    }

    private bool TryFindRandomExitTile(out int tileIdx)
    {
        List<int> exits = [];

        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (item.Logic is FurnitureFreezeExitLogic && item is IRoomFloorItem floor)
            {
                exits.Add(_roomGrain.MapModule.ToIdx(floor.X, floor.Y));
            }
        }

        if (exits.Count == 0)
        {
            tileIdx = -1;

            return false;
        }

        tileIdx = exits[Random.Shared.Next(exits.Count)];

        return true;
    }

    private async Task RefreshGateCountersAsync()
    {
        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (item.Logic is not FurnitureFreezeGateLogic gate)
            {
                continue;
            }

            await gate.SetStateAsync(_game.GetTeamCount(gate.TeamColor));
        }
    }

    /// <summary>Pushes each team's live score to its <c>es_score_*</c> scoreboard (furniture_score shows
    /// the raw state as a number).</summary>
    private async Task RefreshScoreboardsAsync()
    {
        foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
        {
            if (item.Logic is not FurnitureFreezeCounterLogic counter)
            {
                continue;
            }

            await counter.SetStateAsync(_game.GetTeamScore(counter.TeamColor));
        }
    }

    private Task BroadcastEffectAsync(PlayerId playerId, int effectId)
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

    // The room Freeze game has no bespoke HUD protocol: the client shows "game mode" from the generic
    // YouArePlayingGame message, and a number over an avatar from the generic GamePlayerValue message
    // (used here for the remaining lives). Everything else the player sees is avatar effects + furni.

    private Task SetPlayingModeAsync(PlayerId playerId, bool isPlaying) =>
        _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(playerId)
            .SendComposerAsync(new YouArePlayingGameMessageComposer { IsPlaying = isPlaying });

    /// <summary>Shows <paramref name="value"/> as a number bubble over the player's avatar (0 clears it).
    /// Used for the remaining-lives display.</summary>
    private Task BroadcastPlayerValueAsync(PlayerId playerId, int value)
    {
        if (!_roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId))
        {
            return Task.CompletedTask;
        }

        return _roomGrain.SendComposerToRoomAsync(
            new GamePlayerValueMessageComposer { UserId = objectId, Value = value }
        );
    }
}
