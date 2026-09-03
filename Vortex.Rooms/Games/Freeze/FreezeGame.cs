using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Rooms.Games.Abstractions;
using Vortex.Rooms.Games.Arena;
using Vortex.Rooms.Games.Events;
using Vortex.Rooms.Games.Presentation;
using Vortex.Rooms.Games.Scoring;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.Freeze;

/// <summary>
/// Freeze. Players pick a gate, take a loadout of snowballs and lives, and throw at arena tiles; a
/// blast propagates along arms, freezes whoever it catches, shatters ice blocks and sometimes drops a
/// power-up. The last team standing wins, or the timer decides.
/// <para>
/// The three things worth knowing about the shape:
/// <list type="bullet">
/// <item>The blast geometry is a pure function (<see cref="FreezeBlastGeometry"/>) — a radius, a
/// shape and a centre in, coordinates out. It is not dozens of nested coordinate checks inside a
/// furni class, and it is tested on its own.</item>
/// <item>A snowball is a deferred blast on a time-ordered queue stamped with the match that threw it,
/// so one thrown in the last second of a match cannot land in the next one.</item>
/// <item>Per-player state (lives, ammo, boosts, timers) is the roster's; teams and scores are the
/// room's. Neither is duplicated.</item>
/// </list>
/// </para>
/// </summary>
[RoomGame]
public sealed class FreezeGame(IRoomGameContext context) : RoomGameModule(context)
{
    private readonly FreezeRoster _roster = new(context.Teams);

    // A snowball's flight: the blast lands BlastDelayMs after the throw, then the ripple resets
    // ResetDelayMs after that. Time-ordered queues drained each room tick.
    private readonly PriorityQueue<FreezeBlast, long> _blasts = new();
    private readonly PriorityQueue<TileReset, long> _resets = new();

    private FreezeSettings _settings = FreezeSettings.Default;
    private TeamLayout _layout = TeamLayout.FourColours;

    // The 1 s freeze/shield countdown inside the 50 ms room tick. Not readonly — GameCadence is a
    // mutable struct; a readonly field would silently advance a copy.
    private GameCadence _playerTick = new(FreezeConstants.FreezeTickMs);

    // Armed at kick-off only when two or more teams have players, so the match can end the moment one
    // team is wiped out — while a solo or one-team match still runs to the timer instead of instantly
    // "winning".
    private bool _endEarlyArmed;

    public override GameProfile Profile { get; } =
        new() { Id = FreezeConstants.Game, Teams = TeamLayout.FourColours };

    /// <summary>A snowball in flight. The match id is what makes it impossible for a blast to land in
    /// a match that did not throw it.</summary>
    private readonly record struct FreezeBlast(
        MatchId Match,
        int X,
        int Y,
        int Radius,
        bool Diagonal,
        PlayerId Thrower
    );

    private readonly record struct TileReset(MatchId Match, List<int> Tiles);

    // ---- validation --------------------------------------------------------

    public override ArenaValidation ValidateArena() =>
        ArenaValidation
            .Builder()
            .Require("Freeze tiles", _context.Arena.CountOf<IArenaTileComponent>())
            .Prefer("Team gates", _context.Arena.CountOf<ITeamGateComponent>(), required: 2)
            .Prefer("Exit tile", _context.Arena.CountOf<IArenaExitComponent>())
            .Build();

    // ---- lifecycle ---------------------------------------------------------

    public override async Task OnPreparingAsync(GameMatch match, CancellationToken ct)
    {
        _settings = await FreezeConfig.ResolveAsync(_context);
        _layout = TeamLayout.FourColours with
        {
            Capacity = _settings.MaxPlayersPerTeam,
            MinimumTeams = 2,
        };

        _blasts.Clear();
        _resets.Clear();
        _playerTick.Reset();
        _roster.ResetLoadouts(_settings);

        await ResetBlocksAsync();
    }

    public override async Task OnStartedAsync(GameMatch match, CancellationToken ct)
    {
        _endEarlyArmed = _roster.LivingTeamCount() >= _layout.MinimumTeams;

        foreach ((PlayerId playerId, FreezePlayerState player) in _roster.Players)
        {
            await _context.Chrome.SetPlayingModeAsync(playerId, true);
            await _context.Chrome.BroadcastEffectAsync(playerId, player.CurrentEffect());
            await _context.Chrome.BroadcastPlayerValueAsync(playerId, player.Lives);
        }

        await RefreshGateCountersAsync();
    }

    public override async Task OnRoundEndingAsync(GameMatch match, CancellationToken ct)
    {
        _endEarlyArmed = false;
        _blasts.Clear();
        _resets.Clear();

        foreach ((PlayerId playerId, _) in _roster.Players)
        {
            // A match ending mid-freeze must thaw everyone — a lock that outlived the match would
            // strand a player until a wired unfreeze box happened to fire.
            _context.Chrome.UnlockMovement(playerId);
            await _context.Chrome.ClearEffectAsync(playerId);
            await _context.Chrome.BroadcastPlayerValueAsync(playerId, 0);
            await _context.Chrome.SetPlayingModeAsync(playerId, false);
        }

        await RefreshGateCountersAsync();
    }

    public override Task OnResettingAsync(GameMatch match, CancellationToken ct)
    {
        _blasts.Clear();
        _resets.Clear();

        return Task.CompletedTask;
    }

    // ---- tick --------------------------------------------------------------

    public override async Task TickAsync(long now, CancellationToken ct)
    {
        if (!IsLive)
        {
            return;
        }

        MatchId match = _context.Match;

        while (_blasts.TryPeek(out FreezeBlast blast, out long blastDue) && blastDue <= now)
        {
            _blasts.Dequeue();

            if (blast.Match == match)
            {
                await HandleBlastAsync(blast, now, ct);
            }
        }

        while (_resets.TryPeek(out TileReset reset, out long resetDue) && resetDue <= now)
        {
            _resets.Dequeue();

            if (reset.Match == match)
            {
                await ResetTilesAsync(reset.Tiles);
            }
        }

        // A match that started with two or more teams ends the moment only one is left standing.
        if (_endEarlyArmed && _roster.LivingTeamCount() <= 1)
        {
            await _context.RequestMatchEndAsync(ct);

            return;
        }

        if (_playerTick.Due(now))
        {
            await TickPlayersAsync();
        }
    }

    private async Task TickPlayersAsync()
    {
        // A snapshot: thawing a player broadcasts, and an await can let the roster change underneath
        // a live enumeration.
        foreach (
            (PlayerId playerId, FreezePlayerState player) in new List<
                KeyValuePair<PlayerId, FreezePlayerState>
            >(_roster.Players)
        )
        {
            if (!player.Tick())
            {
                continue;
            }

            if (!player.IsFrozen)
            {
                _context.Chrome.UnlockMovement(playerId);
            }

            await _context.Chrome.BroadcastEffectAsync(playerId, player.CurrentEffect());
        }
    }

    // ---- signals -----------------------------------------------------------

    public override Task OnSignalAsync(GameSignal signal, CancellationToken ct) =>
        signal switch
        {
            { Kind: GameSignalKind.WalkOn, Component: ITeamGateComponent gate } =>
                OnGateWalkOnAsync(signal.Player, gate, ct),
            { Kind: GameSignalKind.WalkOn, Component: IArenaExitComponent } => OnForfeitAsync(
                signal.Player,
                ct
            ),
            { Kind: GameSignalKind.WalkOn, Component: IDestructibleComponent block } =>
                OnBlockWalkOnAsync(signal.Player, block, ct),
            { Kind: GameSignalKind.Use, Component: IArenaTileComponent tile } => ThrowBallAsync(
                signal.Player,
                tile,
                ct
            ),
            { Kind: GameSignalKind.Detached, Component: IArenaTileComponent } =>
                OnArenaTileDetachedAsync(ct),
            _ => Task.CompletedTask,
        };

    private async Task OnGateWalkOnAsync(
        PlayerId playerId,
        ITeamGateComponent gate,
        CancellationToken ct
    )
    {
        TeamGateResult result = _roster.ToggleGate(
            _layout,
            playerId,
            gate.Team,
            acceptingPlayers: !HasMatch,
            _settings
        );

        if (result == TeamGateResult.None)
        {
            return;
        }

        await _context.Chrome.BroadcastTeamAuraAsync(
            playerId,
            GameAuraSet.Freeze,
            result == TeamGateResult.Joined ? gate.Team : GameTeamColor.None
        );

        await _context.PublishAsync(
            result == TeamGateResult.Joined
                ? new GameParticipantJoinedEvent { Player = playerId, Team = gate.Team }
                : new GameParticipantLeftEvent { Player = playerId, Team = gate.Team },
            ct
        );

        await RefreshGateCountersAsync();
    }

    /// <summary>A player walked onto an exit tile: they leave the match, and their effect and the
    /// gate counters are cleared. A no-op for anyone not playing.</summary>
    private async Task OnForfeitAsync(PlayerId playerId, CancellationToken ct)
    {
        if (_roster.Get(playerId) is null)
        {
            return;
        }

        GameTeamColor team = _context.Teams.GetTeam(playerId);

        _context.Chrome.UnlockMovement(playerId);
        await _context.Chrome.ClearEffectAsync(playerId);
        await _context.Chrome.BroadcastPlayerValueAsync(playerId, 0);
        await _context.Chrome.SetPlayingModeAsync(playerId, false);

        _roster.Remove(playerId);

        await _context.PublishAsync(
            new GameParticipantLeftEvent { Player = playerId, Team = team },
            ct
        );

        await RefreshGateCountersAsync();
    }

    /// <summary>The rink was dismantled mid-match. A Freeze match with no tiles left has nothing to
    /// throw at and no way to end on its own rules, so it ends here.</summary>
    private async Task OnArenaTileDetachedAsync(CancellationToken ct)
    {
        if (!IsLive || _context.Arena.CountOf<IArenaTileComponent>() > 0)
        {
            return;
        }

        await _context.PublishAsync(
            new GameArenaInvalidatedEvent { Reason = "the last Freeze tile was picked up" },
            ct
        );
        await _context.RequestMatchEndAsync(ct);
    }

    public override Task OnParticipantLeftAsync(PlayerId playerId, CancellationToken ct)
    {
        if (_roster.Remove(playerId) is null)
        {
            return Task.CompletedTask;
        }

        // The playing-game flag is session-scoped, so it must be cleared here too — leaving the room
        // by any means other than the exit tile would otherwise strand the client in "game mode".
        // The fire-and-forget variant is mandatory on this path; the chrome carries the why.
        _context.Chrome.SetPlayingModeAndForget(playerId, false);

        return RefreshGateCountersAsync();
    }

    // ---- throwing ----------------------------------------------------------

    /// <summary>A player double-clicked an arena tile: launch a snowball at it if the rules allow.
    /// The client sends the intent; every check that decides whether it happens is here.</summary>
    private async Task ThrowBallAsync(
        PlayerId playerId,
        IArenaTileComponent target,
        CancellationToken ct
    )
    {
        if (!IsLive)
        {
            return;
        }

        if (
            _roster.Get(playerId) is not FreezePlayerState player
            || !player.CanThrow
            || !_context.TryGetPlayerPosition(playerId, out int throwerX, out int throwerY)
        )
        {
            return;
        }

        // Must be the thrower's own tile or one adjacent (Chebyshev <= 1). A tile further away is a
        // client that made something up, not a throw.
        if (
            Math.Max(Math.Abs(throwerX - target.X), Math.Abs(throwerY - target.Y)) > 1
            || !_context.InBounds(target.X, target.Y)
        )
        {
            return;
        }

        // Only onto an idle arena tile — never mid-animation.
        if (target.GetState() != FreezeConstants.TileIdle)
        {
            return;
        }

        player.SpendSnowball();

        await _context.FacePlayerAsync(playerId, target.X, target.Y);

        int radius = player.TakeThrowRadius();
        bool diagonal = player.NextDiagonal;
        player.NextDiagonal = false;

        // Rise animation now (the ball rises to height radius + 1); the blast lands BlastDelayMs later.
        await target.SetStateAsync((radius + 1) * FreezeConstants.StateWireScale);

        _blasts.Enqueue(
            new FreezeBlast(_context.Match, target.X, target.Y, radius, diagonal, playerId),
            _context.NowMs + FreezeConstants.BlastDelayMs
        );
    }

    private async Task HandleBlastAsync(FreezeBlast blast, long now, CancellationToken ct)
    {
        FreezePlayerState? thrower = _roster.Get(blast.Thrower);
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
            // Bounds-check before ToIdx: a blast arm can project off the map, and ToIdx does no
            // bounds check, so a negative or overflowing coordinate would alias onto another tile.
            if (!_context.InBounds(x, y))
            {
                continue;
            }

            int idx = _context.ToIdx(x, y);

            // Arena tiles flash and freeze whoever stands on them...
            if (_context.Arena.OnTile<IArenaTileComponent>(idx) is IArenaTileComponent tile)
            {
                await tile.SetStateAsync(
                    FreezeConstants.TileBlast * FreezeConstants.StateWireScale
                );
                animated.Add(idx);

                await FreezeOccupantsAsync(idx, thrower, ct);
            }

            // ...and an ice block on the tile is shattered, maybe dropping a power-up.
            await DestroyBlockAsync(idx, thrower, ct);
        }

        if (animated.Count > 0)
        {
            _resets.Enqueue(
                new TileReset(blast.Match, animated),
                now + FreezeConstants.ResetDelayMs - FreezeConstants.BlastDelayMs
            );
        }
    }

    private async Task FreezeOccupantsAsync(
        int tileIdx,
        FreezePlayerState? thrower,
        CancellationToken ct
    )
    {
        foreach (PlayerId occupant in _context.PlayersOn(tileIdx))
        {
            if (_roster.Get(occupant) is not FreezePlayerState victim || !victim.CanBeFrozen)
            {
                continue;
            }

            // Freezing an enemy scores; catching your own team (or yourself) is a friendly-fire
            // penalty. The thrower is on the score event either way, so an own goal stays visible.
            if (thrower is not null)
            {
                bool friendlyFire = victim.Team == thrower.Team;
                int points = friendlyFire
                    ? -_settings.FreezePlayerPoints
                    : _settings.FreezePlayerPoints;
                ScoreReason reason = friendlyFire
                    ? FreezeScoreReasons.FriendlyFire
                    : FreezeScoreReasons.PlayerFrozen;

                await _context.ScoreAsync(
                    new GameScore(thrower.Team, thrower.PlayerId, points, reason, default),
                    ct
                );
            }

            if (victim.Freeze())
            {
                await EliminateAsync(victim, ct);

                continue;
            }

            // Frozen means frozen: rooted in place until the thaw. This is the same lock the wired
            // freeze-user box uses, so "frozen" means one thing in the room.
            _context.Chrome.LockMovement(victim.PlayerId);
            await _context.Chrome.BroadcastEffectAsync(victim.PlayerId, victim.CurrentEffect());
            await _context.Chrome.BroadcastPlayerValueAsync(victim.PlayerId, victim.Lives);
        }
    }

    private async Task EliminateAsync(FreezePlayerState victim, CancellationToken ct)
    {
        // An eliminated player leaves the arena — never with a movement lock still on them.
        _context.Chrome.UnlockMovement(victim.PlayerId);
        await _context.Chrome.ClearEffectAsync(victim.PlayerId);
        await _context.Chrome.BroadcastPlayerValueAsync(victim.PlayerId, 0);
        await _context.Chrome.SetPlayingModeAsync(victim.PlayerId, false);

        List<int> exits = _context.Arena.TilesOf<IArenaExitComponent>();

        if (exits.Count > 0)
        {
            await _context.MovePlayerAsync(victim.PlayerId, _context.Random.Pick(exits));
        }

        _roster.Remove(victim.PlayerId);

        await _context.PublishAsync(
            new GameParticipantEliminatedEvent { Player = victim.PlayerId, Team = victim.Team },
            ct
        );

        await RefreshGateCountersAsync();
    }

    private async Task ResetTilesAsync(List<int> tileIndices)
    {
        foreach (int idx in tileIndices)
        {
            if (_context.Arena.OnTile<IArenaTileComponent>(idx) is IArenaTileComponent tile)
            {
                await tile.SetStateAsync(FreezeConstants.TileIdle);
            }
        }
    }

    // ---- ice blocks and power-ups -------------------------------------------

    /// <summary>Shatters an intact ice block caught in a blast: it rolls the power-up chance and
    /// either reveals a random power-up or breaks empty, scoring the thrower's team for the kill.
    /// Already-broken blocks are inert.</summary>
    private async Task DestroyBlockAsync(int idx, FreezePlayerState? thrower, CancellationToken ct)
    {
        if (
            _context.Arena.OnTile<IDestructibleComponent>(idx) is not IDestructibleComponent block
            || block.GetState() != FreezeConstants.BlockIntact
        )
        {
            return;
        }

        int revealState = FreezeConstants.BlockEmpty;

        if (_context.Random.Chance(_settings.PowerUpChancePercent))
        {
            FreezePowerUp powerUp = FreezePowerUps.Pick(
                _context.Random.Next(FreezePowerUps.Pickable.Length)
            );
            revealState = FreezePowerUps.RevealState(powerUp);
        }

        await block.SetStateAsync(revealState * FreezeConstants.StateWireScale);

        if (thrower is not null)
        {
            await _context.ScoreAsync(
                new GameScore(
                    thrower.Team,
                    thrower.PlayerId,
                    _settings.DestroyBlockPoints,
                    FreezeScoreReasons.BlockDestroyed,
                    block.ObjectId
                ),
                ct
            );
        }
    }

    /// <summary>A player stepped onto a broken block: if it is showing an uncollected power-up,
    /// apply it, score the pick-up and fade the icon out.</summary>
    private async Task OnBlockWalkOnAsync(
        PlayerId playerId,
        IDestructibleComponent block,
        CancellationToken ct
    )
    {
        if (!IsLive || _roster.Get(playerId) is not FreezePlayerState player || player.Dead)
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

        await _context.ScoreAsync(
            new GameScore(
                player.Team,
                playerId,
                _settings.PowerUpPoints,
                FreezeScoreReasons.PowerUpCollected,
                block.ObjectId
            ),
            ct
        );

        await block.SetStateAsync(
            (state + FreezeConstants.BlockCollectedOffset) * FreezeConstants.StateWireScale
        );

        // A shield pick-up changes the effect the player wears; an extra life changes the bubble.
        await _context.Chrome.BroadcastEffectAsync(playerId, player.CurrentEffect());

        if (powerUp == FreezePowerUp.ExtraLife)
        {
            await _context.Chrome.BroadcastPlayerValueAsync(playerId, player.Lives);
        }
    }

    /// <summary>Restores every ice block in the room to intact for a fresh match — the cleanup
    /// contract, run at prepare rather than left to whoever remembers.</summary>
    private async Task ResetBlocksAsync()
    {
        foreach (
            IDestructibleComponent block in _context.Arena.ComponentsOf<IDestructibleComponent>()
        )
        {
            if (block.GetState() != block.IntactState)
            {
                await block.SetStateAsync(block.IntactState);
            }
        }
    }

    private async Task RefreshGateCountersAsync()
    {
        foreach (ITeamGateComponent gate in _context.Arena.ComponentsOf<ITeamGateComponent>())
        {
            await gate.SetStateAsync(_roster.LivingCount(gate.Team));
        }
    }
}
