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
/// is server-decided from where the player was walking, that the ball moves on its own clock, that a
/// goal scores the colour it carries and returns the ball to the spot, and that a ball cannot keep
/// rolling after the match that kicked it ended.
/// </summary>
public sealed class FootballGameTests
{
    private const long Kickoff = 10_000;

    private static FootballBallComponent PlaceBall(RoomHarness harness, int x, int y) =>
        GameFurni.Place(
            harness,
            "football",
            x,
            y,
            (factory, ctx) => new FootballBallComponent(factory, ctx)
        );

    private static FootballGoalComponent PlaceGoal(
        RoomHarness harness,
        string colour,
        int x,
        int y
    ) =>
        GameFurni.Place(
            harness,
            $"football_goal_{colour}",
            x,
            y,
            (factory, ctx) => new FootballGoalComponent(factory, ctx)
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
    public async Task AKickedBall_RollsOneTilePerStep_InTheDirectionThePlayerWasWalking()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 4, 5);
        player.SetRotation(Rotation.East);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await Walk(harness, ball).ConfigureAwait(true);
        await TickAsync(harness, Kickoff + FootballSettings.Default.BallStepMs)
            .ConfigureAwait(true);

        ball.X.Should().Be(6, "the ball is server-authoritative and moves one tile per step");
        ball.Y.Should().Be(5);
    }

    [Fact]
    public async Task AKick_DoesNotMoveTheBallInTheSameTurnAsTheStep()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 4, 5);
        player.SetRotation(Rotation.East);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await Walk(harness, ball).ConfigureAwait(true);

        // The kicker is standing on the ball's tile at this instant; moving it now animates it out
        // from under the step the client has not finished playing.
        ball.X.Should().Be(5);
    }

    [Fact]
    public async Task ABallStopsAtTheRoomEdge()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 9, 5);
        player.SetRotation(Rotation.East);
        FootballBallComponent ball = PlaceBall(harness, 10, 5);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);
        await Walk(harness, ball).ConfigureAwait(true);

        for (int step = 1; step <= FootballSettings.Default.KickDistance; step++)
        {
            await TickAsync(harness, Kickoff + (FootballSettings.Default.BallStepMs * step))
                .ConfigureAwait(true);
        }

        ball.X.Should().Be(11, "the map is 12 wide, so column 11 is the last one");
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
        PlaceGoal(harness, "blue", 2, 5);

        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        harness.Grain.GameRuntime.PhaseOf(FootballConstants.Game).Should().Be(GamePhase.Idle);
    }

    [Fact]
    public async Task AGoal_ScoresTheColourTheGoalCarries_AndReturnsTheBallToTheSpot()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 4, 5);
        player.SetRotation(Rotation.East);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        FootballGoalComponent red = PlaceGoal(harness, "red", 7, 5);
        PlaceGoal(harness, "blue", 2, 5);
        FootballGateComponent gate = PlaceGate(harness, "red", 3, 3);
        await Walk(harness, gate).ConfigureAwait(true);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);
        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);

        await Walk(harness, ball).ConfigureAwait(true);

        for (int step = 1; step <= 3; step++)
        {
            await TickAsync(harness, Kickoff + (FootballSettings.Default.BallStepMs * step))
                .ConfigureAwait(true);
        }

        harness
            .Grain.GameRuntime.GetTeamScore(GameTeamColor.Red)
            .Should()
            .Be(FootballSettings.Default.GoalPoints);
        red.GetState().Should().Be(FootballConstants.GoalScoredState);
        ball.X.Should().Be(7, "the ball sits in the net while the goal is shown");

        await TickAsync(harness, Kickoff + FootballSettings.Default.GoalResetMs + 1_000)
            .ConfigureAwait(true);

        ball.X.Should().Be(5, "and then goes back to where the match started it");
        red.GetState().Should().Be(FootballConstants.GoalIdleState);
    }

    [Fact]
    public async Task AMatchEnding_StopsAMovingBall()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 4, 5);
        player.SetRotation(Rotation.East);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        PlaceGoal(harness, "red", 9, 5);
        PlaceGoal(harness, "blue", 1, 5);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);
        await harness.Grain.GameRuntime.StartGameAsync(CancellationToken.None).ConfigureAwait(true);
        await Walk(harness, ball).ConfigureAwait(true);
        await TickAsync(harness, Kickoff + FootballSettings.Default.BallStepMs)
            .ConfigureAwait(true);
        int stoppedAt = ball.X;

        await harness.Grain.GameRuntime.EndGameAsync(CancellationToken.None).ConfigureAwait(true);

        for (int step = 2; step <= FootballSettings.Default.KickDistance; step++)
        {
            await TickAsync(harness, Kickoff + (FootballSettings.Default.BallStepMs * step))
                .ConfigureAwait(true);
        }

        ball.X.Should().Be(stoppedAt, "a ball cannot keep rolling after its match ended");
    }

    [Fact]
    public async Task AGoalOutsideAMatch_MovesTheBallButScoresNothing()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar player = harness.PutRealPlayerOnTile(RoomHarness.Stranger, 4, 5);
        player.SetRotation(Rotation.East);
        FootballBallComponent ball = PlaceBall(harness, 5, 5);
        PlaceGoal(harness, "red", 7, 5);
        await TickAsync(harness, Kickoff).ConfigureAwait(true);

        await Walk(harness, ball).ConfigureAwait(true);

        for (int step = 1; step <= 3; step++)
        {
            await TickAsync(harness, Kickoff + (FootballSettings.Default.BallStepMs * step))
                .ConfigureAwait(true);
        }

        // A football in an ordinary room is a toy: it goes in, the net reacts, nothing is scored.
        ball.X.Should().Be(7);
        harness.Grain.GameRuntime.GetTeamScore(GameTeamColor.Red).Should().Be(0);
    }

    private static Task TickAsync(RoomHarness harness, long nowMs) =>
        harness.Grain.GameRuntime.TickAsync(nowMs, CancellationToken.None);

    private static Task Walk(RoomHarness harness, IGameComponent component) =>
        harness.Grain.GameRuntime.SignalAsync(
            GameSignal.WalkOn(component, RoomHarness.Stranger),
            CancellationToken.None
        );
}
