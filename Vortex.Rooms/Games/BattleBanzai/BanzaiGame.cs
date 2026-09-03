using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.Abstractions;
using Vortex.Rooms.Games.Arena;
using Vortex.Rooms.Games.Events;
using Vortex.Rooms.Games.Presentation;
using Vortex.Rooms.Games.Scoring;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.BattleBanzai;

/// <summary>
/// Battle Banzai. The module is rules and the arena's visual state, and nothing else: the phases,
/// the teams, the scores, the arena index and every packet belong to the framework.
/// <para>
/// The claim/lock state machine and the enclosure flood fill live in the pure
/// <see cref="BanzaiBoard"/>, which has no room, no packets and no async at all — so every rule
/// this game has is unit-testable without standing a room up.
/// </para>
/// <para>
/// Painting is spread: locking a large enclosed region can change hundreds of tiles at once, and
/// every furni state change publishes into the room's bounded wired-event queue, so the region is
/// drained <c>LockBatchPerTick</c> tiles per frame instead of flooding it in one.
/// </para>
/// </summary>
[RoomGame]
public sealed class BanzaiGame(IRoomGameContext context) : RoomGameModule(context)
{
    private readonly BanzaiBoard _board = new();

    // Enclosed-region tiles waiting to be painted locked, drained LockBatchPerTick per tick.
    private readonly Queue<(int TileIdx, int State)> _pendingPaints = new();

    // In-flight bb_rnd_tele hops and the teleporters whose "active" flash needs clearing. These are
    // deliberately NOT stamped with a match: the teleporters work outside a round too, so a hop is
    // not part of any match and cannot leak into one.
    private readonly PriorityQueue<PendingTeleport, long> _teleports = new();
    private readonly PriorityQueue<RoomObjectId, long> _teleportResets = new();

    private BanzaiSettings _settings = BanzaiSettings.Default;
    private TeamSet _teams = TeamSet.HabboColours;
    private FlickerJob? _flicker;
    private GameCadence _endCheck = new(1000);

    public override GameProfile Profile { get; } =
        new()
        {
            Id = BanzaiConstants.Game,
            Teams = TeamSet.HabboColours,
            RoundEndMs = BanzaiConstants.RoundEndMs,
        };

    private readonly record struct PendingTeleport(
        PlayerId Player,
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

    // ---- validation --------------------------------------------------------

    public override ArenaValidation ValidateArena() =>
        ArenaValidation
            .Builder()
            .Require("Banzai tiles", _context.Arena.CountOf<IArenaTileComponent>())
            .Prefer("Team gates", _context.Arena.CountOf<ITeamGateComponent>(), required: 2)
            .Build();

    // ---- lifecycle ---------------------------------------------------------

    public override async Task OnPreparingAsync(GameMatch match, CancellationToken ct)
    {
        _settings = await BanzaiConfig.ResolveAsync(_context);
        _teams = TeamSet.HabboColours.WithCapacity(_settings.MaxPlayersPerTeam);

        _pendingPaints.Clear();
        _flicker = null;
        _endCheck.Reset();

        // The arena is whatever tiles the room holds at kick-off; they all light neutral, wiping the
        // previous match's colours.
        List<int> arena = [];

        foreach (IArenaTileComponent tile in _context.Arena.ComponentsOf<IArenaTileComponent>())
        {
            arena.Add(_context.ToIdx(tile.X, tile.Y));

            await tile.SetStateAsync(BanzaiConstants.TileNeutral);
        }

        _board.Activate(arena, _context.MapWidth);
    }

    public override async Task OnStartedAsync(GameMatch match, CancellationToken ct)
    {
        // Gates go unwalkable for the match. Walkability is precomputed into the tile flags, so the
        // flip has to be pushed into them rather than being asked of the logic per step.
        await RecomputeGateTilesAsync();
        await RefreshGateCountersAsync();
    }

    public override async Task OnRoundEndingAsync(GameMatch match, CancellationToken ct)
    {
        // The board paints in Habbo colours because a bb_patch's wire state IS the colour; the
        // winner is a team, so it converts once, here.
        GameTeamColor winner = _context.Palette.ColourOf(_context.Teams.GetLeadingTeam());

        // Neutral tiles go dark; claimed and locked tiles keep their colour until the next kick-off.
        foreach (int idx in _board.Deactivate())
        {
            await PaintTileAsync(idx, BanzaiConstants.TileOff);
        }

        _pendingPaints.Clear();

        if (HabboTeamPalette.IsColour(winner))
        {
            List<int> lockedTiles = _board.LockedTilesOf(winner);

            if (lockedTiles.Count > 0)
            {
                _flicker = new FlickerJob
                {
                    Tiles = lockedTiles,
                    LockedState = BanzaiBoard.LockedStateOf(winner),
                    NextDueMs = _context.NowMs + BanzaiConstants.FlickerIntervalMs,
                };
            }
        }

        await RecomputeGateTilesAsync();
        await RefreshGateCountersAsync();
    }

    public override Task OnResettingAsync(GameMatch match, CancellationToken ct)
    {
        // Nothing from this match may survive it: a half-drained paint queue would repaint tiles the
        // next match has already lit neutral.
        _pendingPaints.Clear();
        _flicker = null;

        return Task.CompletedTask;
    }

    // ---- tick --------------------------------------------------------------

    public override async Task TickAsync(long nowMs, CancellationToken ct)
    {
        await DrainFlickerAsync(nowMs);
        await DrainTeleportsAsync(nowMs, ct);

        if (_teleports.Count > 0 || _teleportResets.Count > 0)
        {
            // Teleport hops outlive a match; re-arm the idle tick for as long as one is in flight.
            _context.KeepTicking();
        }

        if (!IsLive)
        {
            return;
        }

        int budget = Math.Max(1, _settings.LockBatchPerTick);

        while (budget-- > 0 && _pendingPaints.TryDequeue(out (int TileIdx, int State) paint))
        {
            await PaintTileAsync(paint.TileIdx, paint.State);
        }

        // Every tile locked ends the round early — through the runtime, so GAME_ENDS fires, the
        // timer furni resets and the room's other games wind down with it.
        if (_endCheck.Due(nowMs) && _pendingPaints.Count == 0 && _board.AllLocked())
        {
            await _context.RequestMatchEndAsync(ct);
        }
    }

    // ---- signals -----------------------------------------------------------

    public override Task OnSignalAsync(GameSignal signal, CancellationToken ct) =>
        signal switch
        {
            { Kind: GameSignalKind.WalkOn, Component: IArenaTileComponent tile } =>
                OnTileWalkOnAsync(signal.Player, tile, ct),
            { Kind: GameSignalKind.WalkOn, Component: ITeamGateComponent gate } =>
                OnGateWalkOnAsync(signal.Player, gate, ct),
            { Kind: GameSignalKind.WalkOn, Component: IRandomTeleportComponent teleport } =>
                EnqueueTeleportHopAsync(signal.Player, teleport.ObjectId, depth: 0),
            { Kind: GameSignalKind.Detached, Component: IArenaTileComponent tile } =>
                OnTileDetachedAsync(tile, ct),
            _ => Task.CompletedTask,
        };

    /// <summary>A player stepped on an arena tile: claim/advance/hijack per the board rules, and
    /// score through the framework, which repaints the boards and fires SCORE_ACHIEVED.</summary>
    private async Task OnTileWalkOnAsync(
        PlayerId playerId,
        IArenaTileComponent tile,
        CancellationToken ct
    )
    {
        if (!IsLive)
        {
            return;
        }

        int tileIdx = _context.ToIdx(tile.X, tile.Y);
        TeamId team = _context.Teams.GetTeam(playerId);

        // The board speaks in colours because a bb_patch's state IS one; the rules speak in teams.
        // This is the single conversion between them on the claim path.
        GameTeamColor colour = _context.Palette.ColourOf(team);
        BanzaiMarkResult result = _board.Mark(colour, tileIdx);

        if (result.Kind == BanzaiMarkKind.None)
        {
            return;
        }

        await tile.SetStateAsync(result.NewState);

        (int points, ScoreReason reason) = result.Kind switch
        {
            BanzaiMarkKind.Fill => (_settings.PointsFillTile, BanzaiScoreReasons.TileFilled),
            BanzaiMarkKind.Hijack => (_settings.PointsHijackTile, BanzaiScoreReasons.TileHijacked),
            BanzaiMarkKind.Lock => (
                _settings.PointsLockTile * (1 + result.RegionLocked.Count),
                BanzaiScoreReasons.TileLocked
            ),
            _ => (0, ScoreReason.Unspecified),
        };

        if (points != 0)
        {
            await _context.ScoreAsync(
                new GameScore(team, playerId, points, reason, tile.ObjectId),
                ct
            );
        }

        if (result.RegionLocked.Count == 0)
        {
            return;
        }

        int lockedState = BanzaiBoard.LockedStateOf(colour);

        foreach (int idx in result.RegionLocked)
        {
            _pendingPaints.Enqueue((idx, lockedState));
        }

        await _context.PublishAsync(
            new BanzaiRegionLockedEvent { Team = team, TileCount = result.RegionLocked.Count },
            ct
        );
    }

    /// <summary>A player stepped on a team gate: membership through the shared gate rules, aura
    /// through the chrome, member counts onto the gate furni.</summary>
    private async Task OnGateWalkOnAsync(
        PlayerId playerId,
        ITeamGateComponent gate,
        CancellationToken ct
    )
    {
        // A gate is painted one of the four colours; which of THIS game's teams that is, is the
        // palette's answer and nobody else's.
        TeamGateResult result = TeamGateRules.Toggle(
            _context.Teams,
            _teams,
            playerId,
            _context.Palette.TeamOf(gate.Team),
            acceptingPlayers: !HasMatch
        );

        if (result == TeamGateResult.None)
        {
            return;
        }

        await _context.Chrome.BroadcastTeamAuraAsync(
            playerId,
            GameAuraSet.Wired,
            result == TeamGateResult.Joined ? gate.Team : GameTeamColor.None
        );

        await RefreshGateCountersAsync();
    }

    private Task OnTileDetachedAsync(IArenaTileComponent tile, CancellationToken ct)
    {
        if (!HasMatch)
        {
            return Task.CompletedTask;
        }

        _board.Remove(_context.ToIdx(tile.X, tile.Y));

        // The last tile taken out from under a live match ends it rather than leaving a match with
        // no board to play on.
        return _board.TileCount == 0 ? InvalidateAsync(ct) : Task.CompletedTask;
    }

    private async Task InvalidateAsync(CancellationToken ct)
    {
        await _context.PublishAsync(
            new GameArenaInvalidatedEvent { Reason = "the last Banzai tile was picked up" },
            ct
        );
        await _context.RequestMatchEndAsync(ct);
    }

    public override Task OnParticipantLeftAsync(PlayerId playerId, CancellationToken ct) =>
        // Membership is already cleared by the runtime; only the gate member counts need repainting.
        RefreshGateCountersAsync();

    // ---- teleporters -------------------------------------------------------

    private async Task EnqueueTeleportHopAsync(
        PlayerId playerId,
        RoomObjectId sourceItemId,
        int depth
    )
    {
        _context.CancelWalk(playerId);

        if (FindTeleport(sourceItemId) is IRandomTeleportComponent source)
        {
            await source.SetStateAsync(BanzaiConstants.TeleportActiveState);
        }

        _teleports.Enqueue(
            new PendingTeleport(playerId, sourceItemId, depth),
            _context.NowMs + BanzaiConstants.TeleportDelayMs
        );

        _context.KeepTicking();
    }

    private async Task DrainTeleportsAsync(long nowMs, CancellationToken ct)
    {
        while (
            _teleportResets.TryPeek(out RoomObjectId itemId, out long resetDue) && resetDue <= nowMs
        )
        {
            _teleportResets.Dequeue();

            if (FindTeleport(itemId) is IRandomTeleportComponent teleport)
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
        if (FindTeleport(hop.SourceItemId) is IRandomTeleportComponent source)
        {
            await source.SetStateAsync(BanzaiConstants.TeleportIdleState);
        }

        if (!_context.TryGetPlayerTile(hop.Player, out _))
        {
            return;
        }

        // A random OTHER teleporter; a lone teleporter teleports nobody.
        List<IRandomTeleportComponent> destinations = [];

        foreach (
            IRandomTeleportComponent candidate in _context.Arena.ComponentsOf<IRandomTeleportComponent>()
        )
        {
            if (candidate.ObjectId != hop.SourceItemId)
            {
                destinations.Add(candidate);
            }
        }

        if (_context.Random.Pick(destinations) is not IRandomTeleportComponent destination)
        {
            return;
        }

        int destinationIdx = _context.ToIdx(destination.X, destination.Y);

        if (!_context.InBounds(destinationIdx))
        {
            return;
        }

        await _context.MovePlayerAsync(hop.Player, destinationIdx);

        // The destination flashes, then goes idle again.
        await destination.SetStateAsync(BanzaiConstants.TeleportActiveState);
        _teleportResets.Enqueue(
            destination.ObjectId,
            _context.NowMs + BanzaiConstants.TeleportDelayMs
        );
        _context.KeepTicking();

        // Landing on another teleporter chains, capped so two of them cannot bounce an avatar
        // forever. The `_exclude` variant deliberately never chains.
        if (hop.Depth < BanzaiConstants.TeleportChainCap && destination.ChainsOnArrival)
        {
            await EnqueueTeleportHopAsync(hop.Player, destination.ObjectId, hop.Depth + 1);

            return;
        }

        // Landing on an arena tile claims it, exactly as walking there would.
        if (_context.Arena.OnTile<IArenaTileComponent>(destinationIdx) is IArenaTileComponent tile)
        {
            await OnTileWalkOnAsync(hop.Player, tile, ct);
        }
    }

    private IRandomTeleportComponent? FindTeleport(RoomObjectId objectId)
    {
        foreach (
            IRandomTeleportComponent candidate in _context.Arena.ComponentsOf<IRandomTeleportComponent>()
        )
        {
            if (candidate.ObjectId == objectId)
            {
                return candidate;
            }
        }

        return null;
    }

    // ---- painting ----------------------------------------------------------

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
        if (_context.Arena.OnTile<IArenaTileComponent>(tileIdx) is IArenaTileComponent tile)
        {
            await tile.SetStateAsync(state);
        }
    }

    private Task RecomputeGateTilesAsync()
    {
        foreach (ITeamGateComponent gate in _context.Arena.ComponentsOf<ITeamGateComponent>())
        {
            _context.RecomputeTile(gate.X, gate.Y);
        }

        return Task.CompletedTask;
    }

    private async Task RefreshGateCountersAsync()
    {
        foreach (ITeamGateComponent gate in _context.Arena.ComponentsOf<ITeamGateComponent>())
        {
            await gate.SetStateAsync(_context.Teams.GetTeamMemberCount(gate.Team));
        }
    }
}
