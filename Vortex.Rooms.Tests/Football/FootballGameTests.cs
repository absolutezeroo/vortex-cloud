using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Rooms.Games.Football;
using Vortex.Rooms.Games.Football.Components;
using Vortex.Rooms.Object.Avatars.Player;
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

    private static readonly FootballSettings Balance = FootballSettings.Default;

    private static FootballBallComponent PlaceBall(RoomHarness harness, int x, int y) =>
        GameFurni.Place(
            harness,
            "football",
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

    private static FootballGateComponent PlaceGate(
        RoomHarness harness,
        string colour,
        int x,
        int y
    ) =>
        GameFurni.Place(
            harness,
            $"football_gate_{colour}",
            x,
            y,
            (factory, ctx) => new FootballGateComponent(factory, ctx)
        );

    [Fact]
    public async Task ABallStruckHead_On_RollsOneTilePerStep_InTheDirectionThePlayerWasWalking()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        RoomPlayerAvatar player = Striker(harness, 4, 5, Rotation.East, ball);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await Walk(harness, ball).ConfigureAwait(true);
        await TickAsync(harness, Kickoff + Balance.FastStepMs).ConfigureAwait(true);

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

        await Walk(harness, ball).ConfigureAwait(true);

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

        await Walk(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        ball.X.Should().Be(1 + Balance.KickDistance);
        ball
            .GetState()
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

        await Walk(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        ball.X.Should().Be(5 + Balance.DragDistance);
    }

    [Fact]
    public async Task ClickingTheBallFromNextToIt_MovesItTheTackleDistance()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 4, 5);
        player.SetRotation(Rotation.East);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await Use(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        ball.X.Should().Be(5 + Balance.TackleDistance);
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

        await Use(harness, ball).ConfigureAwait(true);
        await RollOutAsync(harness).ConfigureAwait(true);

        ball.X.Should().Be(5);
    }

    [Fact]
    public async Task ARollingBall_ShowsItsRemainingTravel()
    {
        // The furni has no idea it is a football: the client's roll animation is entirely this state,
        // so a ball that never sets it slides across the floor rigid.
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 1, 5);
        Striker(harness, 0, 5, Rotation.East, ball);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await Walk(harness, ball).ConfigureAwait(true);

        ball.GetState().Should().Be(Balance.KickDistance + 1);

        await TickAsync(harness, Kickoff + Balance.FastStepMs).ConfigureAwait(true);

        ball.GetState().Should().Be(Balance.KickDistance);
    }

    [Fact]
    public async Task ABallHittingTheRoomEdge_BouncesBackInsteadOfStoppingDead()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 10, 5);
        Striker(harness, 9, 5, Rotation.East, ball);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await Walk(harness, ball).ConfigureAwait(true);
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

        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        // One goal is a target nobody defends. The refusal is what says so.
        harness.Grain.GameRuntime.PhaseOf(FootballConstants.Game).Should().Be(GamePhase.Idle);
    }

    [Fact]
    public async Task ARoomWithNoBall_DoesNotStartAMatch()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        PlaceGoal(harness, "red", 8, 5);
        PlaceGoal(harness, "blue", 2, 5, Rotation.East);

        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        harness.Grain.GameRuntime.PhaseOf(FootballConstants.Game).Should().Be(GamePhase.Idle);
    }

    [Fact]
    public async Task AGoal_ScoresTheColourTheGoalCarries_AndReturnsTheBallToTheSpot()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        Striker(harness, 4, 5, Rotation.East, ball);
        FootballGoalComponent red = PlaceGoal(harness, "red", 7, 5);
        PlaceGoal(harness, "blue", 2, 5, Rotation.East);
        FootballGateComponent gate = PlaceGate(harness, "red", 3, 3);
        await Walk(harness, gate).ConfigureAwait(true);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);
        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await Walk(harness, ball).ConfigureAwait(true);

        // Two hops to reach the net, stepped by hand: rolling the clock out past the reset would
        // measure the ball's return rather than the goal.
        await TickAsync(harness, Kickoff + 200).ConfigureAwait(true);
        await TickAsync(harness, Kickoff + 400).ConfigureAwait(true);

        harness.Grain.GameRuntime.GetTeamScore(GameTeamColor.Red).Should().Be(Balance.GoalPoints);
        red.GetState().Should().Be(FootballConstants.GoalScoredState);
        ball.X.Should().Be(7, "the ball sits in the net while the goal is shown");

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
        FootballGateComponent gate = PlaceGate(harness, "red", 3, 3);
        await Walk(harness, gate).ConfigureAwait(true);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);
        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await Walk(harness, ball).ConfigureAwait(true);
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
        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);
        await Walk(harness, ball).ConfigureAwait(true);
        await TickAsync(harness, Kickoff + Balance.FastStepMs).ConfigureAwait(true);
        int stoppedAt = ball.X;

        await harness.Grain.GameRuntime.EndGameAsync(CancellationToken.None).ConfigureAwait(true);
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

        await Walk(harness, ball).ConfigureAwait(true);
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
            await TickAsync(harness, Kickoff + (Balance.SlowStepMs * step)).ConfigureAwait(true);
        }
    }

    private static Task TickAsync(RoomHarness harness, long nowMs) =>
        harness.Grain.GameRuntime.TickAsync(nowMs, CancellationToken.None);

    private static Task Walk(RoomHarness harness, IGameComponent component) =>
        harness.Grain.GameRuntime.SignalAsync(
            GameSignal.WalkOn(component, RoomHarness.Stranger),
            CancellationToken.None
        );

    private static Task Use(RoomHarness harness, IGameComponent component) =>
        harness.Grain.GameRuntime.SignalAsync(
            GameSignal.Use(component, RoomHarness.Stranger, 0),
            CancellationToken.None
        );
}
