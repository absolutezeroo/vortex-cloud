using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Rooms.Games.Football;
using Vortex.Rooms.Games.Football.Components;
using Vortex.Rooms.Games.Football.Physics;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Football;

/// <summary>
/// Football end to end through the runtime. The ball's own movement rules are pinned by
/// <c>BallPhysicsTests</c> over a bare grid; what these cover is the rest of the game — that a kick
/// is server-decided from where the player was walking and how far they meant to go, that the ball
/// moves on its own clock and shows its roll, that a goal scores the colour it carries and returns
/// the ball to the spot, and that a ball cannot keep rolling after the match that kicked it ended.
/// </summary>
public sealed class FootballGameTests
{
    private const long Kickoff = 10_000;

    /// <summary>The room frame these tests step in, so a cadence assertion measures what the live
    /// room can actually resolve rather than an interval no tick lands on.</summary>
    private const int RoomTickMs = 50;

    private static readonly FootballSettings Balance = FootballSettings.Default;

    /// <summary>The wait before a struck ball's first hop — the shortest one it ever takes.</summary>
    private static int TopStepMs =>
        BallPhysics.StepDelayMs(BallPhysics.PaceOf(Balance.KickDistance, Balance));

    private static FootballBallComponent PlaceBall(RoomHarness harness, int x, int y) =>
        GameFurni.Place(
            harness,
            "furniture_pushable",
            x,
            y,
            (factory, ctx) => new FootballBallComponent(factory, ctx)
        );

    /// <summary>A net, facing the way its mouth opens. A goal only takes a ball entering that mouth,
    /// so the facing is part of placing one rather than a detail a test may leave out.</summary>
    private static FootballGoalComponent PlaceGoal(
        RoomHarness harness,
        string colour,
        int x,
        int y,
        Rotation facing = Rotation.West
    ) =>
        GameFurni.Place(
            harness,
            $"football_goal_{colour}",
            x,
            y,
            (factory, ctx) => new FootballGoalComponent(factory, ctx),
            rotation: facing
        );

    /// <summary>A team counter. The classname carries the colour and the logic key does not, which is
    /// how the furnidata binds every <c>fball_score_*</c>: on <c>furniture_hockey_score</c>.</summary>
    private static FurnitureHockeyScoreLogic PlaceScoreboard(
        RoomHarness harness,
        string colour,
        int x,
        int y
    ) =>
        GameFurni.Place(
            harness,
            "furniture_hockey_score",
            x,
            y,
            (factory, ctx) => new FurnitureHockeyScoreLogic(factory, ctx),
            classname: $"fball_score_{colour}"
        );

    [Fact]
    public async Task ABallStruckHead_On_RollsOneTilePerStep_InTheDirectionThePlayerWasWalking()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        RoomPlayerAvatar player = Striker(harness, 4, 5, Rotation.East, ball);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);
        await TickAsync(harness, Kickoff + TopStepMs).ConfigureAwait(true);

        player.Should().NotBeNull();
        ball.X.Should().Be(6, "the ball is server-authoritative and moves one tile per step");
        ball.Y.Should().Be(5);
    }

    [Fact]
    public async Task AKick_DoesNotMoveTheBallInTheSameTurnAsTheStep()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        Striker(harness, 4, 5, Rotation.East, ball);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);

        // The kicker is standing on the ball's tile at this instant; moving it now animates it out
        // from under the step the client has not finished playing.
        ball.X.Should().Be(5);
    }

    [Fact]
    public async Task AKick_TravelsTheFullDistance()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 1, 5);
        Striker(harness, 0, 5, Rotation.East, ball);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        ball.X.Should().Be(1 + Balance.KickDistance);
        ball.GetState()
            .Should()
            .Be(FootballConstants.BallRestingState, "a ball at rest is not spinning");
    }

    [Fact]
    public async Task WalkingThroughTheBall_DribblesItOneTileInsteadOfStrikingIt()
    {
        // Habbo's football can be walked along, not only struck. The two are told apart by where the
        // player was heading: at the ball is a kick, past it is a dribble.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 4, 5);
        player.SetRotation(Rotation.East);
        player.SetGoalTileId(harness.Grain.MapModule.ToIdx(9, 5));
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        ball.X.Should().Be(5 + Balance.DragDistance);
    }

    [Fact]
    public async Task DribblingOntoYourOwnDestination_StaysADribbleToTheLastStep()
    {
        // The bug this pins made the dribble unreachable in play. A nudge pushes the ball onto the
        // very tile the walker is heading for, so from the next step on, "my destination is the
        // ball's tile" — the test for a deliberate strike — was true of a ball being pushed along.
        // Every walk that met the ball therefore ended in a full-power shot, and a player who only
        // ever walked forward saw one behaviour: it blasts off.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 1, 5);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 0, 5);
        player.SetRotation(Rotation.East);
        player.SetGoalTileId(harness.Grain.MapModule.ToIdx(3, 5));

        long now = Kickoff;

        // Three steps of one walk to (3,5). The ball is a tile ahead each time, and the last contact
        // happens with the ball standing on the destination itself.
        for (int step = 0; step < 3; step++)
        {
            await WalkAsync(harness, ball).ConfigureAwait(true);

            now = await RollOutFromAsync(harness, now).ConfigureAwait(true);
        }

        ball.X.Should().Be(1 + (3 * Balance.DragDistance), "every step of one walk is a nudge");

        // And aiming at where it now sits is a fresh strike, not more dribbling.
        player.SetGoalTileId(harness.Grain.MapModule.ToIdx(ball.X, ball.Y));

        await WalkAsync(harness, ball).ConfigureAwait(true);
        await RollOutFromAsync(harness, now).ConfigureAwait(true);

        ball.X.Should().Be(4 + Balance.KickDistance);
    }

    [Fact]
    public async Task ClickingTheBallFromNextToIt_MovesItTheTackleDistance()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 4, 5);
        player.SetRotation(Rotation.East);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await UseAsync(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        ball.X.Should().Be(5 + Balance.TackleDistance);
    }

    [Fact]
    public async Task AUseLandingOnABallAlreadyRolling_DoesNotKickItASecondTime()
    {
        // What a double-click actually is: a walk AND a use. The walk-on strikes the ball, then the
        // use arrives while the room still has the walker on the tile BEFORE it — the position is
        // committed on the following tick — so they read as adjacent and the 4-tile tackle replaced
        // the 5-tile kick already under way. The ball was hit twice for one click.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 2, 5);
        Striker(harness, 1, 5, Rotation.East, ball);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);
        await UseAsync(harness, ball).ConfigureAwait(true);
        await RollOutFromAsync(harness, Kickoff).ConfigureAwait(true);

        ball.X.Should().Be(2 + Balance.KickDistance, "a ball in flight is not tackled by a click");
    }

    [Fact]
    public async Task ClickingTheBallFromAcrossTheRoom_DoesNothing()
    {
        // The client sends intent; the server decides. A click from six tiles away is not a tackle.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 1, 1);
        player.SetRotation(Rotation.East);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await UseAsync(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        ball.X.Should().Be(5);
    }

    [Fact]
    public async Task ARollingBall_ComesDownThePaceLadderOneRungPerTile()
    {
        // The client's FurniturePushableLogic reads this state: state/10 divides its own 500ms slide
        // and state%10 is the animation frame. Both digits are the pace, so these four numbers are
        // at once the deceleration a viewer sees and the interval the server hops on. The old model
        // could only produce a gear change — four hops at 125ms, the rest at 500ms.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 1, 5);
        Striker(harness, 0, 5, Rotation.East, ball);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);

        List<int> shown = [];

        for (int ms = 0; ms <= 1_500; ms += RoomTickMs)
        {
            int state = ball.GetState();

            if (
                state != FootballConstants.BallRestingState
                && (shown.Count == 0 || shown[^1] != state)
            )
            {
                shown.Add(state);
            }

            await TickAsync(harness, Kickoff + ms).ConfigureAwait(true);
        }

        shown.Should().Equal(55, 44, 33, 22);
        ball.GetState()
            .Should()
            .Be(FootballConstants.BallRestingState, "and then it stops rolling");
    }

    [Fact]
    public async Task ABallHittingTheRoomEdge_BouncesBackInsteadOfStoppingDead()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 10, 5);
        Striker(harness, 9, 5, Rotation.East, ball);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        // The map is 12 wide, so the ball reaches column 11, turns, and spends the rest of the kick
        // coming back. A ball that stopped at the wall would still be sitting on 11.
        ball.X.Should().BeLessThan(11, "a struck ball bounces off what it cannot pass");
    }

    [Fact]
    public async Task ARoomWithOneGoalColour_DoesNotStartAMatch()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        PlaceBall(harness, 5, 5);
        PlaceGoal(harness, "red", 8, 5);

        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);

        // One goal is a target nobody defends. The refusal is what says so.
        harness.Grain.GameRuntime.PhaseOf(FootballConstants.Game).Should().Be(GamePhase.Idle);
    }

    [Fact]
    public async Task ARoomWithNoBall_DoesNotStartAMatch()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        PlaceGoal(harness, "red", 8, 5);
        PlaceGoal(harness, "blue", 2, 5, Rotation.East);

        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);

        harness.Grain.GameRuntime.PhaseOf(FootballConstants.Game).Should().Be(GamePhase.Idle);
    }

    [Fact]
    public async Task AGoal_ScoresTheColourTheGoalCarries_PaintsItsBoard_AndReturnsTheBallToTheSpot()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        Striker(harness, 4, 5, Rotation.East, ball);
        FootballGoalComponent red = PlaceGoal(harness, "red", 7, 5);
        PlaceGoal(harness, "blue", 2, 5, Rotation.East);
        FurnitureHockeyScoreLogic board = PlaceScoreboard(harness, "r", 3, 3);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);
        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);

        // Two hops to reach the net, stepped by hand: rolling the clock out past the reset would
        // measure the ball's return rather than the goal.
        await TickAsync(harness, Kickoff + 200).ConfigureAwait(true);
        await TickAsync(harness, Kickoff + 400).ConfigureAwait(true);

        harness.Grain.GameRuntime.GetTeamScore(GameTeamColor.Red).Should().Be(Balance.GoalPoints);
        red.GetState().Should().Be(FootballConstants.GoalScoredState);
        ball.X.Should().Be(7, "the ball sits in the net while the goal is shown");

        // The board is the only place a football score is visible: the game has no gates and no
        // participants, so nothing else in the room would ever show that a goal was scored.
        board
            .GetState()
            .Should()
            .Be(Balance.GoalPoints, "a red goal paints the red fball_score board");

        await TickAsync(harness, Kickoff + 400 + Balance.GoalResetMs + 100).ConfigureAwait(true);

        ball.X.Should().Be(5, "and then goes back to where the match started it");
        red.GetState().Should().Be(FootballConstants.GoalIdleState);
    }

    [Fact]
    public async Task ABallRollingIntoTheBackOfANet_DoesNotScore()
    {
        // Without the net's facing, a ball that got behind the goal scored through the woodwork.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        Striker(harness, 4, 5, Rotation.East, ball);
        PlaceGoal(harness, "red", 7, 5, Rotation.East);
        PlaceGoal(harness, "blue", 2, 1, Rotation.East);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);
        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        harness.Grain.GameRuntime.GetTeamScore(GameTeamColor.Red).Should().Be(0);
        ball.X.Should().NotBe(7, "the back of a net is solid");
    }

    [Fact]
    public async Task AMatchEnding_StopsAMovingBall()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        Striker(harness, 4, 5, Rotation.East, ball);
        PlaceGoal(harness, "red", 9, 5);
        PlaceGoal(harness, "blue", 1, 5, Rotation.East);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);
        await harness
            .Grain.GameRuntime.StartGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);
        await WalkAsync(harness, ball).ConfigureAwait(true);
        await TickAsync(harness, Kickoff + TopStepMs).ConfigureAwait(true);
        int stoppedAt = ball.X;

        await harness
            .Grain.GameRuntime.EndGameAsync(default, GameId.None, CancellationToken.None)
            .ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        ball.X.Should().Be(stoppedAt, "a ball cannot keep rolling after its match ended");
    }

    [Fact]
    public async Task AGoalOutsideAMatch_MovesTheBallButScoresNothing()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        Striker(harness, 4, 5, Rotation.East, ball);
        PlaceGoal(harness, "red", 7, 5);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await WalkAsync(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        // A football in an ordinary room is a toy: it goes in, the net reacts, nothing is scored.
        ball.X.Should().Be(7);
        harness.Grain.GameRuntime.GetTeamScore(GameTeamColor.Red).Should().Be(0);
    }

    // ---- helpers ------------------------------------------------------------

    /// <summary>
    /// A player standing beside the ball who was walking AT it — which is what makes their step a
    /// strike rather than a dribble, and is decided by the walk they asked for, not by the client.
    /// </summary>
    private static RoomPlayerAvatar Striker(
        RoomHarness harness,
        int x,
        int y,
        Rotation facing,
        IGameComponent ball
    )
    {
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, x, y);
        player.SetRotation(facing);
        player.SetGoalTileId(harness.Grain.MapModule.ToIdx(ball.X, ball.Y));

        return player;
    }

    /// <summary>Ticks until any ball has finished travelling. The cadence is not uniform, so a test
    /// that stepped the clock by a fixed amount would stop measuring what it meant to.</summary>
    private static async Task RollOutAsync(RoomHarness harness)
    {
        for (int step = 1; step <= 40; step++)
        {
            await TickAsync(harness, Kickoff + (FootballConstants.PushableAnimationTimeMs * step))
                .ConfigureAwait(true);
        }
    }

    /// <summary>Rolls any ball out from a clock that keeps going, and returns where the clock got to.
    /// A test with more than one kick in it cannot re-tick the same timestamps: the second kick would
    /// be scheduled in the future of a clock that never advanced past the first.</summary>
    private static async Task<long> RollOutFromAsync(RoomHarness harness, long fromMs)
    {
        for (int tick = 0; tick < 40; tick++)
        {
            fromMs += RoomTickMs;

            await TickAsync(harness, fromMs).ConfigureAwait(true);
        }

        return fromMs;
    }

    private static Task TickAsync(RoomHarness harness, long nowMs) =>
        harness.Grain.GameRuntime.TickAsync(nowMs, CancellationToken.None);

    private static Task WalkAsync(RoomHarness harness, IGameComponent component) =>
        harness.Grain.GameRuntime.SignalAsync(
            GameSignal.WalkOn(component, RoomHarness.Stranger),
            CancellationToken.None
        );

    private static Task UseAsync(RoomHarness harness, IGameComponent component) =>
        harness.Grain.GameRuntime.SignalAsync(
            GameSignal.Use(component, RoomHarness.Stranger, 0),
            CancellationToken.None
        );
}
