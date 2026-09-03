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
/// <para><b>Server-authoritative.</b> The client says only "a player stepped onto the ball". The
/// direction, the distance, every tile the ball occupies and every goal are decided here.</para>
/// </summary>
[RoomGame]
public sealed class FootballGame(IRoomGameContext context) : RoomGameModule(context), IBallSpace
{
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
        int goalColours = 0;
        HashSet<GameTeamColor> seen = [];

        foreach (IGoalComponent goal in _context.Arena.ComponentsOf<IGoalComponent>())
        {
            if (GameTeamBook.IsRealTeam(goal.Team) && seen.Add(goal.Team))
            {
                goalColours++;
            }
        }

        return ArenaValidation
            .Builder()
            .Require("Football", _context.Arena.CountOf<IBallComponent>())
            .Require("Goals of different colours", goalColours, required: 2)
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
    }

    public override Task OnRoundEndingAsync(GameMatch match, CancellationToken ct) =>
        // A ball cannot keep rolling after its match ends. Stopping them here rather than letting the
        // tick discover it is what makes that an invariant instead of a race.
        StopAllBallsAsync();

    public override Task OnResettingAsync(GameMatch match, CancellationToken ct)
    {
        _balls.Clear();

        return ResetGoalsAsync();
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
        BallStep step = BallPhysics.Advance(fromIdx, motion.Direction, this);

        switch (step.Outcome)
        {
            case BallStepOutcome.Blocked:
                motion.Stop();

                return;

            case BallStepOutcome.Goal:
                await ScoreGoalAsync(ball, motion, step.TileIdx, nowMs, ct);

                return;

            case BallStepOutcome.Rolled:
                await _context.SlideItemAsync(ball, step.TileIdx);

                motion.StepsRemaining--;
                motion.NextStepAtMs = nowMs + _settings.BallStepMs;

                if (motion.StepsRemaining <= 0)
                {
                    motion.Stop();
                }

                return;
        }
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

        motion.Direction = Rotation.None;
        motion.StepsRemaining = 0;
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
            { Kind: GameSignalKind.WalkOn, Component: IBallComponent ball } =>
                KickAsync(signal.Player, ball, ct),
            { Kind: GameSignalKind.WalkOn, Component: ITeamGateComponent gate } =>
                OnGateWalkOnAsync(signal.Player, gate, ct),
            { Kind: GameSignalKind.Detached, Component: IBallComponent ball } =>
                OnBallDetached(ball, ct),
            _ => Task.CompletedTask,
        };

    /// <summary>
    /// A player stepped into a ball. The direction is the one they were walking in — their body
    /// rotation, which the room has already turned toward the tile they stepped onto — and the
    /// distance is config, not anything the client sends.
    /// </summary>
    private Task KickAsync(PlayerId playerId, IBallComponent ball, CancellationToken ct)
    {
        if (!_balls.TryGetValue(ball.ObjectId, out BallMotion? motion))
        {
            // A ball kicked outside a match: it still rolls, it just belongs to no match.
            motion = new BallMotion();
            _balls[ball.ObjectId] = motion;
        }

        // A ball on its way back from a goal is out of play until it is back on the spot.
        if (motion.IsWaitingToReturn)
        {
            return Task.CompletedTask;
        }

        if (!_context.TryGetPlayerFacing(playerId, out Rotation facing) || facing == Rotation.None)
        {
            return Task.CompletedTask;
        }

        motion.Kick(
            _context.Match,
            playerId,
            facing,
            _settings.KickDistance,
            // Next tick, not this one: the kicker is standing on the ball's tile right now, and a
            // ball that moved in the same turn would be animated from under their feet before the
            // client has finished the step that put them there.
            _context.NowMs + _settings.BallStepMs
        );

        _context.KeepTicking();

        return _context.PublishAsync(
            new FootballBallKickedEvent
            {
                Ball = ball.ObjectId,
                Kicker = playerId,
                Direction = facing,
                Distance = _settings.KickDistance,
            },
            ct
        );
    }

    private Task OnBallDetached(IBallComponent ball, CancellationToken ct)
    {
        _balls.Remove(ball.ObjectId);

        // The last ball taken out of a live match ends it: there is nothing left to play with.
        return IsLive && _context.Arena.CountOf<IBallComponent>() == 0
            ? _context.RequestMatchEndAsync(ct)
            : Task.CompletedTask;
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

    bool IBallSpace.HasAvatar(int tileIdx) => HasAvatar(tileIdx);

    bool IBallSpace.IsGoal(int tileIdx) =>
        _context.Arena.OnTile<IGoalComponent>(tileIdx) is not null;

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

        await ResetGoalsAsync();
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
