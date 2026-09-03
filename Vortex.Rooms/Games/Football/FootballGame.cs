using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.Abstractions;
using Vortex.Rooms.Games.Arena;
using Vortex.Rooms.Games.Events;
using Vortex.Rooms.Games.Football.Physics;
using Vortex.Rooms.Games.Presentation;
using Vortex.Rooms.Games.Scoring;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.Football;

/// <summary>
/// Football. A player walking into a ball kicks it; the ball rolls tile by tile until it runs out of
/// travel, hits something, or goes in. The match adds teams, a score and a kickoff spot; without one
/// the balls still work, which is what a football does in an ordinary Habbo room.
/// <para>
/// Four responsibilities, kept apart on purpose:
/// <list type="bullet">
/// <item><b>Ball simulation</b> — <see cref="BallPhysics"/>, a pure function of the grid, tested over
/// a hand-built grid with no room in sight.</item>
/// <item><b>Football rules</b> — this class: what a kick is worth in travel, what a goal scores, when
/// the ball goes back to the spot.</item>
/// <item><b>Room movement</b> — the room's own <c>RollFloorItem</c>, reached through the context.
/// There is no second notion of a coordinate here.</item>
/// <item><b>Presentation</b> — the slide bundle the rollers already send, and the scoreboards, which
/// repaint off the score event.</item>
/// </list>
/// </para>
/// <para><b>Server-authoritative.</b> The client says only "a player stepped onto the ball" or
/// "clicked it". The direction, the distance, every tile the ball occupies and every goal are
/// decided here.</para>
/// <para>
/// <b>Provenance.</b> Habbo's own football is not authoritatively known — there is no capture of the
/// official server, and the official client carries no football logic at all, because a
/// <c>fball</c> is an ordinary floor item the server slides with the same bundle the rollers use.
/// The behaviour below (a struck ball travels further than a dribbled one, it accelerates then
/// slows, it bounces off what it cannot pass, a net only accepts a ball entering its mouth, and a
/// player in the way usually but not always takes it) is what the open-source reference emulator
/// does. Per the repository contract that is <b>evidence, not authority</b>; every number it implies
/// is admin-editable in <see cref="FootballSettings"/> rather than compiled in.
/// </para>
/// </summary>
[RoomGame]
public sealed class FootballGame(IRoomGameContext context) : RoomGameModule(context), IBallSpace
{
    /// <summary>How many times one hop may turn the ball before we let it settle. The reference
    /// emulator recurses without a bound and can spin forever on a ball walled in on both sides; a
    /// cap costs nothing and makes "a ball always comes to rest" an invariant.</summary>
    private const int MaxBouncesPerStep = 8;

    private readonly Dictionary<RoomObjectId, BallMotion> _balls = [];

    private FootballSettings _settings = FootballSettings.Default;
    private TeamLayout _layout = TeamLayout.FourColours;

    public override GameProfile Profile { get; } =
        new() { Id = FootballConstants.Game, Teams = TeamLayout.FourColours };

    // ---- validation --------------------------------------------------------

    /// <summary>
    /// A football match needs a ball and at least two goals of different colours — one goal is a
    /// target nobody defends, and no ball is not a game. The gates are preferred rather than required
    /// because a wired join-team box can put players on teams without any.
    /// </summary>
    public override ArenaValidation ValidateArena()
    {
        return ArenaValidation
            .Builder()
            .Require("Football", _context.Arena.CountOf<IBallComponent>())
            .Require("Goals of different colours", CountGoalColours(), required: 2)
            .Prefer("Team gates", _context.Arena.CountOf<ITeamGateComponent>(), required: 2)
            .Build();
    }

    // ---- lifecycle ---------------------------------------------------------

    public override async Task OnPreparingAsync(GameMatch match, CancellationToken ct)
    {
        _settings = await FootballConfig.ResolveAsync(_context);
        _layout = TeamLayout.FourColours with { Capacity = _settings.MaxPlayersPerTeam };

        _balls.Clear();

        // Wherever each ball is standing when the match is prepared is its kickoff spot, and where it
        // returns to after a goal. There is no separate spawn furni: the ball's own position is the
        // spot, which is how a room owner sets one — by putting the ball where they want it.
        foreach (IBallComponent ball in _context.Arena.ComponentsOf<IBallComponent>())
        {
            _balls[ball.ObjectId] = new BallMotion
            {
                KickoffTileIdx = _context.ToIdx(ball.X, ball.Y),
            };
        }

        await ResetGoalsAsync();
        await RestAllBallsAsync();
    }

    public override Task OnRoundEndingAsync(GameMatch match, CancellationToken ct) =>
        // A ball cannot keep rolling after its match ends. Stopping them here rather than letting the
        // tick discover it is what makes that an invariant instead of a race.
        StopAllBallsAsync();

    public override async Task OnResettingAsync(GameMatch match, CancellationToken ct)
    {
        _balls.Clear();

        await ResetGoalsAsync();
        await RestAllBallsAsync();
    }

    // ---- tick --------------------------------------------------------------

    public override async Task TickAsync(long nowMs, CancellationToken ct)
    {
        if (_balls.Count == 0)
        {
            return;
        }

        MatchId match = _context.Match;
        bool anyBusy = false;

        // A snapshot: a goal ends the match, which clears the dictionary from under a live loop.
        foreach (RoomObjectId ballId in new List<RoomObjectId>(_balls.Keys))
        {
            if (!_balls.TryGetValue(ballId, out BallMotion? motion))
            {
                continue;
            }

            if (motion.IsIdle)
            {
                continue;
            }

            anyBusy = true;

            // A kick from a previous match cannot move a ball in this one.
            if (motion.Match != match)
            {
                motion.Stop();

                continue;
            }

            if (motion.IsWaitingToReturn)
            {
                if (nowMs >= motion.ReturnAtMs)
                {
                    await ReturnBallToSpotAsync(ballId, motion);
                }

                continue;
            }

            if (nowMs < motion.NextStepAtMs)
            {
                continue;
            }

            await StepBallAsync(ballId, motion, nowMs, ct);
        }

        if (anyBusy)
        {
            // Balls roll outside a match too, so the idle tick has to be re-armed for as long as one
            // is moving — and stops being armed the moment none is.
            _context.KeepTicking();
        }
    }

    /// <summary>
    /// One hop. A ball that cannot go where it was headed turns rather than stops — that is the whole
    /// of the bounce — and only rests when it has run out of travel, been taken off a player's feet,
    /// or has nowhere left to turn. Bouncing costs neither a hop nor a beat, exactly as the reference
    /// does, but is bounded here so a ball boxed into a single tile settles instead of spinning.
    /// </summary>
    private async Task StepBallAsync(
        RoomObjectId ballId,
        BallMotion motion,
        long nowMs,
        CancellationToken ct
    )
    {
        if (FindBall(ballId) is not IBallComponent ball)
        {
            // The furni went away mid-roll.
            _balls.Remove(ballId);

            return;
        }

        int fromIdx = _context.ToIdx(ball.X, ball.Y);
        int delayMs = BallPhysics.StepDelayMs(motion.CurrentStep, motion.TotalSteps, _settings);

        for (int attempt = 0; attempt <= MaxBouncesPerStep; attempt++)
        {
            BallStep step = BallPhysics.Advance(fromIdx, motion.Direction, this);

            switch (step.Outcome)
            {
                case BallStepOutcome.Goal:
                    await ScoreGoalAsync(ball, motion, step.TileIdx, nowMs, ct);

                    return;

                case BallStepOutcome.Rolled:
                    await _context.SlideItemAsync(ball, step.TileIdx);

                    motion.NextStepAtMs = nowMs + delayMs;
                    motion.StepsRemaining--;

                    if (motion.StepsRemaining <= 0)
                    {
                        await RestAsync(ball, motion);
                    }
                    else
                    {
                        await ball.SetStateAsync(BallPhysics.RollState(motion.StepsRemaining));
                    }

                    return;

                case BallStepOutcome.Tackled:
                    // Somebody's feet, not a wall. It stops dead where it is.
                    await RestAsync(ball, motion);

                    return;

                case BallStepOutcome.Blocked:
                    if (!motion.CanBounce)
                    {
                        await RestAsync(ball, motion);

                        return;
                    }

                    Rotation bounced = BallPhysics.Bounce(fromIdx, motion.Direction, this);

                    if (bounced == Rotation.None || bounced == motion.Direction)
                    {
                        await RestAsync(ball, motion);

                        return;
                    }

                    motion.Direction = bounced;

                    break;
            }
        }

        // Boxed in: it turned as many times as we allow and still had nowhere to go.
        await RestAsync(ball, motion);
    }

    private async Task ScoreGoalAsync(
        IBallComponent ball,
        BallMotion motion,
        int goalTileIdx,
        long nowMs,
        CancellationToken ct
    )
    {
        await _context.SlideItemAsync(ball, goalTileIdx);
        await ball.SetStateAsync(FootballConstants.BallRestingState);

        motion.Direction = Rotation.None;
        motion.TotalSteps = 0;
        motion.StepsRemaining = 0;
        motion.CanBounce = false;
        motion.NextStepAtMs = 0;

        IGoalComponent? goal = _context.Arena.OnTile<IGoalComponent>(goalTileIdx);

        if (goal is null)
        {
            // The goal was picked up between the physics call and here; nothing to score.
            motion.Stop();

            return;
        }

        await goal.SetStateAsync(FootballConstants.GoalScoredState);

        // Only a goal scored inside a live match counts. Outside one the ball still goes in and the
        // net still reacts — a football in an ordinary room is a toy, not a scoreless bug.
        if (IsLive)
        {
            await _context.ScoreAsync(
                new GameScore(
                    goal.Team,
                    motion.LastKicker,
                    _settings.GoalPoints,
                    FootballScoreReasons.Goal,
                    goal.ObjectId
                ),
                ct
            );

            await _context.PublishAsync(
                new FootballGoalScoredEvent
                {
                    Team = goal.Team,
                    Kicker = motion.LastKicker,
                    KickerTeam = _context.Teams.GetTeam(motion.LastKicker),
                    Goal = goal.ObjectId,
                },
                ct
            );
        }

        if (_settings.GoalResetMs <= 0)
        {
            // Configured to leave the ball in the net, which is what the reference emulator does:
            // somebody walks it back out. The goal keeps its scored state until the match resets it.
            motion.Stop();

            return;
        }

        motion.ReturnAtMs = nowMs + _settings.GoalResetMs;

        _context.KeepTicking();
    }

    private async Task ReturnBallToSpotAsync(RoomObjectId ballId, BallMotion motion)
    {
        motion.ReturnAtMs = 0;

        await ResetGoalsAsync();

        if (motion.KickoffTileIdx < 0 || FindBall(ballId) is not IBallComponent ball)
        {
            motion.Stop();

            return;
        }

        // Only if the spot is still free — a ball that materialised inside a stack somebody built
        // there while the goal celebration ran would be stuck.
        if (IsOpen(motion.KickoffTileIdx) && !HasAvatar(motion.KickoffTileIdx))
        {
            await _context.SlideItemAsync(ball, motion.KickoffTileIdx);
        }

        motion.Stop();
    }

    // ---- signals -----------------------------------------------------------

    public override Task OnSignalAsync(GameSignal signal, CancellationToken ct) =>
        signal switch
        {
            { Kind: GameSignalKind.WalkOn, Component: IBallComponent ball } => KickAsync(
                signal.Player,
                ball,
                ct
            ),
            { Kind: GameSignalKind.Use, Component: IBallComponent ball } => TackleAsync(
                signal.Player,
                ball,
                ct
            ),
            { Kind: GameSignalKind.WalkOn, Component: ITeamGateComponent gate } =>
                OnGateWalkOnAsync(signal.Player, gate, ct),
            { Kind: GameSignalKind.Detached, Component: IBallComponent ball } =>
                OnBallDetachedAsync(ball, ct),
            { Kind: GameSignalKind.Detached, Component: IGoalComponent } => OnGoalDetachedAsync(ct),
            _ => Task.CompletedTask,
        };

    /// <summary>
    /// A player stepped into a ball. The direction is the one they were walking in — their body
    /// rotation, which the room has already turned toward the tile they stepped onto.
    /// <para>
    /// How far it goes depends on what they were doing. Walking AT the ball — the tile they were
    /// heading for is the ball's own — strikes it the full distance; crossing that tile on the way
    /// somewhere else only nudges it along, and a nudge does not bounce off walls. That distinction
    /// is what makes dribbling possible at all, and it is why the walker's goal tile is asked for
    /// rather than assumed.
    /// </para>
    /// </summary>
    private Task KickAsync(PlayerId playerId, IBallComponent ball, CancellationToken ct)
    {
        bool struck =
            _context.TryGetPlayerGoalTile(playerId, out int goalTileIdx)
            && goalTileIdx == _context.ToIdx(ball.X, ball.Y);

        return SetBallMovingAsync(
            playerId,
            ball,
            struck ? _settings.KickDistance : _settings.DragDistance,
            canBounce: struck,
            ct
        );
    }

    /// <summary>
    /// A player clicked the ball from beside it, without stepping on it. It travels less far than a
    /// run-up kick and, being struck rather than nudged, bounces. Only from an adjacent tile: a click
    /// from across the room is the client asking for something the server will not do.
    /// </summary>
    private Task TackleAsync(PlayerId playerId, IBallComponent ball, CancellationToken ct)
    {
        if (!_context.TryGetPlayerPosition(playerId, out int x, out int y))
        {
            return Task.CompletedTask;
        }

        return IsAdjacent(x, y, ball)
            ? SetBallMovingAsync(playerId, ball, _settings.TackleDistance, canBounce: true, ct)
            : Task.CompletedTask;
    }

    private static bool IsAdjacent(int x, int y, IBallComponent ball)
    {
        int dx = x - ball.X;
        int dy = y - ball.Y;

        return (dx, dy) != (0, 0) && dx is >= -1 and <= 1 && dy is >= -1 and <= 1;
    }

    private async Task SetBallMovingAsync(
        PlayerId playerId,
        IBallComponent ball,
        int distance,
        bool canBounce,
        CancellationToken ct
    )
    {
        if (distance <= 0)
        {
            return;
        }

        if (!_balls.TryGetValue(ball.ObjectId, out BallMotion? motion))
        {
            // A ball kicked outside a match: it still rolls, it just belongs to no match.
            motion = new BallMotion();
            _balls[ball.ObjectId] = motion;
        }

        // A ball on its way back from a goal is out of play until it is back on the spot.
        if (motion.IsWaitingToReturn)
        {
            return;
        }

        if (!_context.TryGetPlayerFacing(playerId, out Rotation facing) || facing == Rotation.None)
        {
            return;
        }

        motion.Kick(
            _context.Match,
            playerId,
            facing,
            distance,
            canBounce,
            // Next hop, not this turn: the kicker is standing on the ball's tile right now, and a
            // ball that moved in the same turn would be animated from under their feet before the
            // client has finished the step that put them there.
            _context.NowMs + BallPhysics.StepDelayMs(1, distance, _settings)
        );

        await ball.SetStateAsync(BallPhysics.RollState(distance));

        _context.KeepTicking();

        await _context.PublishAsync(
            new FootballBallKickedEvent
            {
                Ball = ball.ObjectId,
                Kicker = playerId,
                Direction = facing,
                Distance = distance,
            },
            ct
        );
    }

    private Task OnBallDetachedAsync(IBallComponent ball, CancellationToken ct)
    {
        _balls.Remove(ball.ObjectId);

        // The last ball taken out of a live match ends it: there is nothing left to play with.
        return IsLive && _context.Arena.CountOf<IBallComponent>() == 0
            ? InvalidateAsync("the last football was picked up", ct)
            : Task.CompletedTask;
    }

    /// <summary>A goal was picked up mid-match. Two colours is the minimum a match validated on, so
    /// falling under it ends the match rather than leaving one side with nothing to defend.</summary>
    private Task OnGoalDetachedAsync(CancellationToken ct) =>
        IsLive && CountGoalColours() < 2
            ? InvalidateAsync("a goal was picked up, leaving fewer than two colours", ct)
            : Task.CompletedTask;

    private async Task InvalidateAsync(string reason, CancellationToken ct)
    {
        await _context.PublishAsync(new GameArenaInvalidatedEvent { Reason = reason }, ct);
        await _context.RequestMatchEndAsync(ct);
    }

    private int CountGoalColours()
    {
        HashSet<GameTeamColor> seen = [];

        foreach (IGoalComponent goal in _context.Arena.ComponentsOf<IGoalComponent>())
        {
            if (GameTeamBook.IsRealTeam(goal.Team))
            {
                seen.Add(goal.Team);
            }
        }

        return seen.Count;
    }

    private async Task OnGateWalkOnAsync(
        PlayerId playerId,
        ITeamGateComponent gate,
        CancellationToken ct
    )
    {
        TeamGateResult result = TeamGateRules.Toggle(
            _context.Teams,
            _layout,
            playerId,
            gate.Team,
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

        await _context.PublishAsync(
            result == TeamGateResult.Joined
                ? new GameParticipantJoinedEvent { Player = playerId, Team = gate.Team }
                : new GameParticipantLeftEvent { Player = playerId, Team = gate.Team },
            ct
        );

        await RefreshGateCountersAsync();
    }

    public override Task OnParticipantLeftAsync(PlayerId playerId, CancellationToken ct) =>
        RefreshGateCountersAsync();

    // ---- IBallSpace: the room, as the physics needs it ----------------------

    bool IBallSpace.TryStep(int fromTileIdx, Rotation direction, out int nextTileIdx) =>
        _context.TryGetTileInFront(fromTileIdx, direction, out nextTileIdx);

    bool IBallSpace.IsOpen(int tileIdx) => IsOpen(tileIdx);

    /// <summary>Rolled, not decided: a player does not stop every ball that reaches them. The roll
    /// goes through the match's seeded random, so a replayed match intercepts the same balls.</summary>
    bool IBallSpace.AvatarStopsBall(int tileIdx) =>
        HasAvatar(tileIdx) && _context.Random.Chance(_settings.AvatarStopChancePercent);

    bool IBallSpace.TryGetGoal(int tileIdx, out Rotation facing)
    {
        IGoalComponent? goal = _context.Arena.OnTile<IGoalComponent>(tileIdx);

        facing = goal?.Facing ?? Rotation.None;

        return goal is not null;
    }

    private bool IsOpen(int tileIdx) => _context.IsTileOpenForItem(tileIdx);

    private bool HasAvatar(int tileIdx) => _context.HasAvatarOn(tileIdx);

    // ---- helpers -----------------------------------------------------------

    private IBallComponent? FindBall(RoomObjectId objectId)
    {
        foreach (IBallComponent ball in _context.Arena.ComponentsOf<IBallComponent>())
        {
            if (ball.ObjectId == objectId)
            {
                return ball;
            }
        }

        return null;
    }

    private async Task StopAllBallsAsync()
    {
        foreach (BallMotion motion in _balls.Values)
        {
            motion.Stop();
        }

        await RestAllBallsAsync();
        await ResetGoalsAsync();
    }

    /// <summary>Stops a ball and clears the roll state, so the client puts it down instead of leaving
    /// it spinning on the floor forever.</summary>
    private static async Task RestAsync(IBallComponent ball, BallMotion motion)
    {
        motion.Stop();

        if (ball.GetState() != FootballConstants.BallRestingState)
        {
            await ball.SetStateAsync(FootballConstants.BallRestingState);
        }
    }

    private async Task RestAllBallsAsync()
    {
        foreach (IBallComponent ball in _context.Arena.ComponentsOf<IBallComponent>())
        {
            if (ball.GetState() != FootballConstants.BallRestingState)
            {
                await ball.SetStateAsync(FootballConstants.BallRestingState);
            }
        }
    }

    private async Task ResetGoalsAsync()
    {
        foreach (IGoalComponent goal in _context.Arena.ComponentsOf<IGoalComponent>())
        {
            if (goal.GetState() != FootballConstants.GoalIdleState)
            {
                await goal.SetStateAsync(FootballConstants.GoalIdleState);
            }
        }
    }

    private async Task RefreshGateCountersAsync()
    {
        foreach (ITeamGateComponent gate in _context.Arena.ComponentsOf<ITeamGateComponent>())
        {
            await gate.SetStateAsync(_context.Teams.GetTeamMemberCount(gate.Team));
        }
    }
}
