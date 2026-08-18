using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
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
public sealed class RoomFreezeSystem(RoomGrain roomGrain) : RoomMinigameBase(roomGrain)
{
    public override string Name => "freeze";

    // Teams and scores are the room's shared GameTeamState, not a second store of our own: the wired
    // team conditions/selectors and the SCORE_ACHIEVED trigger all read that one, and a Freeze round
    // that kept its own would be invisible to every one of them.
    private readonly RoomFreezeGame _game = new() { Teams = roomGrain.GameSystem.TeamState };

    // A snowball's flight: the blast lands BlastDelayMs after the throw, then the ripple resets
    // ResetDelayMs after that. Kept as time-ordered queues drained each room tick.
    private readonly PriorityQueue<FreezeBlast, long> _blasts = new();
    private readonly PriorityQueue<List<int>, long> _resets = new();

    private long _currentTickMs;

    // The 1s freeze/shield countdown inside the 50ms room tick. Not readonly — GameCadence is a
    // mutable struct; a readonly field would silently advance a copy.
    private GameCadence _playerTick = new(FreezeConstants.FreezeTickMs);

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

    // ---- lifecycle ---------------------------------------------------------

    public async Task OnGateWalkOnAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct)
    {
        FreezeGateResult result = _game.ToggleGate(playerId, team);

        if (result == FreezeGateResult.None)
        {
            return;
        }

        await _roomGrain.GameChrome.BroadcastTeamAuraAsync(
            playerId,
            GameAuraSet.Freeze,
            result == FreezeGateResult.Joined ? team : GameTeamColor.None
        );

        await RefreshGateCountersAsync();
    }

    /// <summary>Kicks off a round for whoever is standing on the gates. Driven by
    /// <see cref="RoomGameSystem"/>, which has already cleared the shared scores and fired GAME_STARTS
    /// by the time this runs — nothing calls it directly.</summary>
    public override async Task StartAsync(CancellationToken ct)
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
        _playerTick.Reset();
        _endEarlyArmed = _game.LivingTeamCount() >= 2;

        await ResetBlocksAsync();

        foreach ((PlayerId playerId, FreezePlayerState player) in _game.Players)
        {
            await _roomGrain.GameChrome.SetPlayingModeAsync(playerId, true);
            await _roomGrain.GameChrome.BroadcastEffectAsync(playerId, player.CurrentEffect());
            await _roomGrain.GameChrome.BroadcastPlayerValueAsync(playerId, player.Lives);
        }

        // The scoreboards repaint themselves: RoomGameScoreboardSystem reacts to the round events
        // and every score change this game makes through AddTeamScoreAsync.
        await RefreshGateCountersAsync();
    }

    /// <summary>Winds the round down: clears effects, ammo and the in-flight snowballs, and leaves the
    /// scoreboards showing the final tally. Driven by <see cref="RoomGameSystem"/> after GAME_ENDS has
    /// fired. The winner stays readable from the shared scores afterwards.</summary>
    public override async Task EndAsync(CancellationToken ct)
    {
        _game.Stop();

        _endEarlyArmed = false;
        _blasts.Clear();
        _resets.Clear();

        foreach ((PlayerId playerId, _) in _game.Players)
        {
            // A round ending mid-freeze must thaw everyone — a lock that outlives the round would
            // strand a player until a wired unfreeze box happened to fire.
            _roomGrain.GameChrome.UnlockMovement(playerId);
            await _roomGrain.GameChrome.ClearEffectAsync(playerId);
            await _roomGrain.GameChrome.BroadcastPlayerValueAsync(playerId, 0);
            await _roomGrain.GameChrome.SetPlayingModeAsync(playerId, false);
        }

        await RefreshGateCountersAsync();
    }

    public override Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct)
    {
        if (_game.Remove(playerId) is null)
        {
            return Task.CompletedTask;
        }

        // The playing-game flag is session-scoped, so it must be cleared here too — leaving the room
        // by any means other than the exit tile would otherwise strand the client in "game mode".
        // The fire-and-forget variant is mandatory on this path; the chrome carries the why.
        _roomGrain.GameChrome.SetPlayingModeAndForget(playerId, false);

        return RefreshGateCountersAsync();
    }

    /// <summary>A player walked onto an exit tile: they leave the game (forfeit), and their effect and the
    /// gate counters are cleared. No-op for anyone not in the game.</summary>
    public async Task OnExitWalkOnAsync(PlayerId playerId, CancellationToken ct)
    {
        if (_game.GetPlayer(playerId) is null)
        {
            return;
        }

        await _roomGrain.GameChrome.ClearEffectAsync(playerId);
        await _roomGrain.GameChrome.BroadcastPlayerValueAsync(playerId, 0);
        await _roomGrain.GameChrome.SetPlayingModeAsync(playerId, false);
        _game.Remove(playerId);
        await RefreshGateCountersAsync();
    }

    /// <summary>Room-tick entry: lands due snowball blasts, resets finished ripples and runs the 1s
    /// freeze/shield countdown. Cheap when no game is running.</summary>
    public override async Task TickAsync(long now, CancellationToken ct)
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
        // Ending goes through the coordinator, never straight to our own EndAsync: that is what fires
        // wf_trg_game_ends on an early finish — previously only a timer running out reached it — and
        // what stops any other game in the room from being left running on its own.
        if (_endEarlyArmed && _game.LivingTeamCount() <= 1)
        {
            await _roomGrain.GameSystem.EndGameAsync(ct);
            _roomGrain.GameChrome.ResetGameTimers();

            return;
        }

        if (_playerTick.Due(now))
        {
            await TickPlayersAsync(ct);
        }
    }

    private async Task TickPlayersAsync(CancellationToken ct)
    {
        foreach ((PlayerId playerId, FreezePlayerState player) in _game.Players.ToList())
        {
            if (player.Tick())
            {
                if (!player.IsFrozen)
                {
                    _roomGrain.GameChrome.UnlockMovement(playerId);
                }

                await _roomGrain.GameChrome.BroadcastEffectAsync(playerId, player.CurrentEffect());
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

        // Must be the thrower's own tile or one adjacent (Chebyshev <= 1), and on the map (an edge
        // thrower could otherwise aim at an off-map coordinate that ToIdx aliases onto a real tile).
        if (
            Math.Max(Math.Abs(thrower!.X - targetX), Math.Abs(thrower.Y - targetY)) > 1
            || !_roomGrain.MapModule.InBounds(targetX, targetY)
        )
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
            // Bounds-check before ToIdx: a blast arm can project off the map, and ToIdx does no bounds
            // check, so a negative/overflowing coordinate would otherwise alias onto an unrelated tile.
            if (!_roomGrain.MapModule.InBounds(x, y))
            {
                continue;
            }

            int idx = _roomGrain.MapModule.ToIdx(x, y);

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

                await AddTeamScoreAsync(thrower.Team, points, ct);
            }

            bool died = victim.Freeze();

            if (died)
            {
                await EliminateAsync(victim, avatar, ct);
            }
            else
            {
                // Frozen means frozen: rooted in place until the thaw (Habbo behaviour — this is
                // the same lock the wired freeze-user box uses).
                _roomGrain.GameChrome.LockMovement(victim.PlayerId);
                await _roomGrain.GameChrome.BroadcastEffectAsync(
                    victim.PlayerId,
                    victim.CurrentEffect()
                );
                await _roomGrain.GameChrome.BroadcastPlayerValueAsync(
                    victim.PlayerId,
                    victim.Lives
                );
            }
        }
    }

    private async Task EliminateAsync(
        FreezePlayerState victim,
        IRoomAvatar avatar,
        CancellationToken ct
    )
    {
        // An eliminated player leaves the arena — never with a movement lock still on them.
        _roomGrain.GameChrome.UnlockMovement(victim.PlayerId);
        await _roomGrain.GameChrome.ClearEffectAsync(victim.PlayerId);
        await _roomGrain.GameChrome.BroadcastPlayerValueAsync(victim.PlayerId, 0);
        await _roomGrain.GameChrome.SetPlayingModeAsync(victim.PlayerId, false);

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
            await AddTeamScoreAsync(thrower.Team, _game.Settings.DestroyBlockPoints, ct);
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
        await AddTeamScoreAsync(player.Team, _game.Settings.PowerUpPoints, ct);

        await block.SetStateAsync(
            (state + FreezeConstants.BlockCollectedOffset) * FreezeConstants.StateWireScale
        );

        // A shield pick-up changes the effect the player wears; an extra life changes the lives bubble.
        await _roomGrain.GameChrome.BroadcastEffectAsync(playerId, player.CurrentEffect());

        if (powerUp == FreezePowerUp.ExtraLife)
        {
            await _roomGrain.GameChrome.BroadcastPlayerValueAsync(playerId, player.Lives);
        }
    }

    /// <summary>Restores every ice block in the room to intact for a fresh round.</summary>
    private async Task ResetBlocksAsync()
    {
        foreach (
            FurnitureFreezeBlockLogic block in _roomGrain._state.ItemIndex.LogicsOf<FurnitureFreezeBlockLogic>()
        )
        {
            if (block.GetState() != FreezeConstants.BlockIntact)
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

    private FurnitureFreezeTileLogic? FindFreezeTile(int tileIdx) =>
        _roomGrain.MapModule.FirstLogicOnTile<FurnitureFreezeTileLogic>(tileIdx);

    private FurnitureFreezeBlockLogic? FindFreezeBlock(int tileIdx) =>
        _roomGrain.MapModule.FirstLogicOnTile<FurnitureFreezeBlockLogic>(tileIdx);

    private bool TryFindRandomExitTile(out int tileIdx)
    {
        List<int> exits = [];

        foreach (IRoomItem item in _roomGrain._state.ItemIndex.ItemsOf<FurnitureFreezeExitLogic>())
        {
            if (item is IRoomFloorItem floor)
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
        foreach (
            FurnitureFreezeGateLogic gate in _roomGrain._state.ItemIndex.LogicsOf<FurnitureFreezeGateLogic>()
        )
        {
            await gate.SetStateAsync(_game.GetTeamCount(gate.TeamColor));
        }
    }

    /// <summary>Scores for a team through the room's shared game system, so the change fires the wired
    /// SCORE_ACHIEVED trigger exactly as a <c>wf_act_give_score</c> box would.</summary>
    private Task AddTeamScoreAsync(GameTeamColor team, int amount, CancellationToken ct) =>
        _roomGrain.GameSystem.AddTeamScoreAsync(team, amount, ct);

    // The room Freeze game has no bespoke HUD protocol: the client shows "game mode" from the generic
    // YouArePlayingGame message, and a number over an avatar from the generic GamePlayerValue message
    // (used here for the remaining lives). Everything else the player sees is avatar effects + furni —
    // all of it sent through the shared RoomGameChrome.
}
