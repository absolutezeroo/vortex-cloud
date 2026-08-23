using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Grains.Systems.Banzai;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Banzai;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Grain-side controller for the room's Battle Banzai minigame — the thin IO wrapper around the
/// pure <see cref="RoomBanzaiGame"/> (board, phase, gate rules), composed entirely from the shared
/// bricks: teams and scores live in the room's <see cref="GameTeamState"/>, auras go through
/// <see cref="RoomGameChrome"/> (the wired 33-36 set — Banzai IS the wired-aura game), scoreboards
/// repaint via <see cref="RoomGameScoreboardSystem"/> off the score events, and the arena furni are
/// found through the item index. All calls run inside the room grain's single-threaded turn.
/// <para>
/// The enclosed-region lock is painted through a bounded per-tick queue: every furni state change
/// publishes a wired event into a bounded queue (512/64-per-tick), so a giant fill is spread over
/// ticks instead of flooding it. Gates are physically unwalkable while a round runs — walkability
/// is precomputed into the tile flags, so start/end explicitly recompute each gate's tile.
/// </para>
/// </summary>
public sealed class RoomBanzaiSystem(RoomGrain roomGrain) : RoomMinigameBase(roomGrain)
{
    public override string Name => "banzai";

    private readonly RoomBanzaiGame _game = new() { Teams = roomGrain.GameSystem.TeamState };

    // Enclosed-region tiles waiting to be painted locked, drained LockBatchPerTick per tick.
    private readonly Queue<(int TileIdx, int State)> _pendingPaints = new();

    // In-flight bb_rnd_tele hops and the teleporters whose "active" flash needs clearing.
    private readonly PriorityQueue<PendingTeleport, long> _teleports = new();
    private readonly PriorityQueue<RoomObjectId, long> _teleportResets = new();

    // The winner's locked tiles blink at round end; one job at a time.
    private FlickerJob? _flicker;

    private GameCadence _endCheck = new(1000);
    private long _currentTickMs;

    private readonly record struct PendingTeleport(
        RoomObjectId AvatarId,
        RoomObjectId SourceItemId,
        int Depth
    );

    private sealed class FlickerJob
    {
        public required List<int> Tiles { get; init; }
        public required int LockedState { get; init; }
        public int StepsRemaining { get; set; } = BanzaiConstants.FlickerCount;
        public long NextDueMs { get; set; }
    }

    public bool IsRoundRunning => _game.IsRunning;

    public override async Task StartAsync(CancellationToken ct)
    {
        _game.Settings = await BanzaiConfig.ResolveAsync(
            _roomGrain._grainFactory.GetServerConfigGrain()
        );

        if (!_game.Start())
        {
            return;
        }

        _pendingPaints.Clear();
        _flicker = null;
        _endCheck.Reset();

        // The arena is whatever bb_patch tiles the room holds at kick-off; they all light neutral,
        // wiping last round's colours.
        List<int> arena = [];

        foreach (IRoomItem item in _roomGrain._state.ItemIndex.ItemsOf<FurnitureBanzaiTileLogic>())
        {
            if (item is IRoomFloorItem floor && item.Logic is FurnitureBanzaiTileLogic tile)
            {
                int idx = _roomGrain.MapModule.ToIdx(floor.X, floor.Y);
                arena.Add(idx);

                await tile.SetStateAsync(BanzaiConstants.TileNeutral);
            }
        }

        _game.Board.Activate(arena, _roomGrain.MapModule.Width);

        await RecomputeGateTilesAsync();
        await RefreshGateCountersAsync();
    }

    public override async Task EndAsync(CancellationToken ct)
    {
        GameTeamColor winner = _game.Stop();

        // Neutral tiles go dark; claimed/locked keep their colour until the next kick-off.
        foreach (int idx in _game.Board.Deactivate())
        {
            await PaintTileAsync(idx, BanzaiConstants.TileOff);
        }

        _pendingPaints.Clear();

        if (GameTeamState.IsRealTeam(winner))
        {
            List<int> lockedTiles = _game.Board.LockedTilesOf(winner);

            if (lockedTiles.Count > 0)
            {
                _flicker = new FlickerJob
                {
                    Tiles = lockedTiles,
                    LockedState = BanzaiBoard.LockedStateOf(winner),
                    NextDueMs = _currentTickMs + BanzaiConstants.FlickerIntervalMs,
                };
            }
        }

        await RecomputeGateTilesAsync();
        await RefreshGateCountersAsync();
    }

    public override async Task TickAsync(long nowMs, CancellationToken ct)
    {
        _currentTickMs = nowMs;

        // The end-of-round celebration and teleport animations keep running while idle, so these
        // drain before the running check.
        await DrainFlickerAsync(nowMs);
        await DrainTeleportsAsync(nowMs, ct);

        if (!_game.IsRunning)
        {
            return;
        }

        int budget = Math.Max(1, _game.Settings.LockBatchPerTick);

        while (budget-- > 0 && _pendingPaints.TryDequeue(out (int TileIdx, int State) paint))
        {
            await PaintTileAsync(paint.TileIdx, paint.State);
        }

        // Every tile locked ends the round early — through the coordinator, so GAME_ENDS fires and
        // the room's other games wind down with it; the timer furni resets like any early end.
        if (_endCheck.Due(nowMs) && _pendingPaints.Count == 0 && _game.Board.AllLocked())
        {
            await _roomGrain.GameSystem.EndGameAsync(ct);
            _roomGrain.GameChrome.ResetGameTimers();
        }
    }

    public override Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct) =>
        // Membership is already cleared from the shared store by the coordinator; only the gate
        // member counts need repainting.
        RefreshGateCountersAsync();

    /// <summary>A player stepped on a bb_patch tile: claim/advance/hijack per the board rules,
    /// score through the shared path (which repaints scoreboards and fires SCORE_ACHIEVED).</summary>
    public async Task OnTileWalkOnAsync(PlayerId playerId, int tileIdx, CancellationToken ct)
    {
        if (!_game.IsRunning)
        {
            return;
        }

        GameTeamColor team = _roomGrain.GameSystem.GetTeam(playerId);
        BanzaiMarkResult result = _game.Board.Mark(team, tileIdx);

        if (result.Kind == BanzaiMarkKind.None)
        {
            return;
        }

        await PaintTileAsync(tileIdx, result.NewState);

        int points = result.Kind switch
        {
            BanzaiMarkKind.Fill => _game.Settings.PointsFillTile,
            BanzaiMarkKind.Hijack => _game.Settings.PointsHijackTile,
            BanzaiMarkKind.Lock => _game.Settings.PointsLockTile * (1 + result.RegionLocked.Count),
            _ => 0,
        };

        if (points != 0)
        {
            await _roomGrain.GameSystem.AddTeamScoreAsync(team, points, ct);
        }

        int lockedState = BanzaiBoard.LockedStateOf(team);

        foreach (int idx in result.RegionLocked)
        {
            _pendingPaints.Enqueue((idx, lockedState));
        }
    }

    /// <summary>A player stepped on a team gate: toggle membership (idle only), aura via the shared
    /// chrome, member counts onto the gate furni.</summary>
    public async Task OnGateWalkOnAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct)
    {
        BanzaiGateResult result = _game.ToggleGate(playerId, team);

        if (result == BanzaiGateResult.None)
        {
            return;
        }

        await _roomGrain.GameChrome.BroadcastTeamAuraAsync(
            playerId,
            GameAuraSet.Wired,
            result == BanzaiGateResult.Joined ? team : GameTeamColor.None
        );

        await RefreshGateCountersAsync();
    }

    /// <summary>A player stepped on a bb_rnd_tele: flash it and, half a second later, whisk them to
    /// a random other teleporter in the room.</summary>
    public async Task OnTeleportWalkOnAsync(
        PlayerId playerId,
        RoomObjectId sourceItemId,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId avatarId))
        {
            return;
        }

        await EnqueueTeleportHopAsync(avatarId, sourceItemId, depth: 0);
    }

    private async Task EnqueueTeleportHopAsync(
        RoomObjectId avatarId,
        RoomObjectId sourceItemId,
        int depth
    )
    {
        if (
            _roomGrain._state.AvatarsByObjectId.TryGetValue(avatarId, out IRoomAvatar? avatar)
            && avatar is not null
        )
        {
            _roomGrain.AvatarModule.CancelWalk(avatar);
        }

        if (
            _roomGrain._state.ItemsById.TryGetValue(sourceItemId, out IRoomItem? source)
            && source.Logic is FurnitureBanzaiTeleportLogic sourceLogic
        )
        {
            await sourceLogic.SetStateAsync(BanzaiConstants.TeleportActiveState);
        }

        _teleports.Enqueue(
            new PendingTeleport(avatarId, sourceItemId, depth),
            _currentTickMs + BanzaiConstants.TeleportDelayMs
        );
    }

    private async Task DrainTeleportsAsync(long nowMs, CancellationToken ct)
    {
        while (
            _teleportResets.TryPeek(out RoomObjectId itemId, out long resetDue) && resetDue <= nowMs
        )
        {
            _teleportResets.Dequeue();

            if (
                _roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item)
                && item.Logic is FurnitureBanzaiTeleportLogic teleport
            )
            {
                await teleport.SetStateAsync(BanzaiConstants.TeleportIdleState);
            }
        }

        while (_teleports.TryPeek(out PendingTeleport hop, out long due) && due <= nowMs)
        {
            _teleports.Dequeue();
            await ExecuteTeleportHopAsync(hop, ct);
        }
    }

    private async Task ExecuteTeleportHopAsync(PendingTeleport hop, CancellationToken ct)
    {
        // The source stops flashing whether or not the hop still works.
        if (
            _roomGrain._state.ItemsById.TryGetValue(hop.SourceItemId, out IRoomItem? source)
            && source.Logic is FurnitureBanzaiTeleportLogic sourceLogic
        )
        {
            await sourceLogic.SetStateAsync(BanzaiConstants.TeleportIdleState);
        }

        if (!_roomGrain._state.AvatarsByObjectId.TryGetValue(hop.AvatarId, out IRoomAvatar? avatar))
        {
            return;
        }

        // A random OTHER teleporter; a lone teleporter teleports nobody.
        List<IRoomFloorItem> destinations = [];

        foreach (
            IRoomItem item in _roomGrain._state.ItemIndex.ItemsOf<FurnitureBanzaiTeleportLogic>()
        )
        {
            if (item.ObjectId != hop.SourceItemId && item is IRoomFloorItem floor)
            {
                destinations.Add(floor);
            }
        }

        if (destinations.Count == 0)
        {
            return;
        }

        IRoomFloorItem destination = destinations[Random.Shared.Next(destinations.Count)];
        int destinationIdx = _roomGrain.MapModule.ToIdx(destination.X, destination.Y);

        if (destinationIdx < 0 || destinationIdx >= _roomGrain._state.TileHeights.Length)
        {
            return;
        }

        _roomGrain.MapModule.RollAvatar(
            avatar,
            destinationIdx,
            _roomGrain._state.TileHeights[destinationIdx]
        );

        await _roomGrain.SendComposerToRoomAsync(
            new UserUpdateMessageComposer { Avatars = [avatar.GetSnapshot()] }
        );

        // The destination flashes, then goes idle again.
        if (destination.Logic is FurnitureBanzaiTeleportLogic destinationLogic)
        {
            await destinationLogic.SetStateAsync(BanzaiConstants.TeleportActiveState);
            _teleportResets.Enqueue(
                destination.ObjectId,
                _currentTickMs + BanzaiConstants.TeleportDelayMs
            );

            // Landing on another teleporter chains, capped so two of them cannot bounce an avatar
            // forever. The `_exclude` variant deliberately never chains — the documented reading of
            // battlebanzai_random_teleport_exclude (an assumption; Arcturus daybreak has one class).
            if (
                avatar is IRoomPlayer
                && hop.Depth < BanzaiConstants.TeleportChainCap
                && !destinationLogic.IsExclude
            )
            {
                await EnqueueTeleportHopAsync(hop.AvatarId, destination.ObjectId, hop.Depth + 1);

                return;
            }
        }

        // Landing on an arena tile claims it, exactly as walking there would.
        if (
            avatar is IRoomPlayer player
            && _roomGrain.MapModule.FirstLogicOnTile<FurnitureBanzaiTileLogic>(destinationIdx)
                is not null
        )
        {
            await OnTileWalkOnAsync(player.PlayerId, destinationIdx, ct);
        }
    }

    private async Task DrainFlickerAsync(long nowMs)
    {
        if (_flicker is null || nowMs < _flicker.NextDueMs)
        {
            return;
        }

        _flicker.StepsRemaining--;

        // Even steps show the locked colour, odd steps blank — the last step always lands lit.
        int state =
            _flicker.StepsRemaining % 2 == 0 ? _flicker.LockedState : BanzaiConstants.TileOff;

        foreach (int idx in _flicker.Tiles)
        {
            await PaintTileAsync(idx, state);
        }

        if (_flicker.StepsRemaining <= 0)
        {
            _flicker = null;

            return;
        }

        _flicker.NextDueMs = nowMs + BanzaiConstants.FlickerIntervalMs;
    }

    private async Task PaintTileAsync(int tileIdx, int state)
    {
        if (
            _roomGrain.MapModule.FirstLogicOnTile<FurnitureBanzaiTileLogic>(tileIdx)
            is FurnitureBanzaiTileLogic tile
        )
        {
            await tile.SetStateAsync(state);
        }
    }

    /// <summary>Gates are unwalkable while a round runs; walkability is baked into the tile flags,
    /// so every gate's tile is recomputed when the phase flips.</summary>
    private Task RecomputeGateTilesAsync()
    {
        foreach (IRoomItem item in _roomGrain._state.ItemIndex.ItemsOf<FurnitureBanzaiGateLogic>())
        {
            if (item is IRoomFloorItem floor)
            {
                _roomGrain.MapModule.ComputeTile(floor.X, floor.Y);
            }
        }

        return Task.CompletedTask;
    }

    private async Task RefreshGateCountersAsync()
    {
        foreach (
            FurnitureBanzaiGateLogic gate in _roomGrain._state.ItemIndex.LogicsOf<FurnitureBanzaiGateLogic>()
        )
        {
            await gate.SetStateAsync(_game.Teams.GetTeamMemberCount(gate.TeamColor));
        }
    }
}
