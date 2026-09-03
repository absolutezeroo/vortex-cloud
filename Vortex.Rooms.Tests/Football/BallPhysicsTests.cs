using System.Collections.Generic;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Rooms.Games.Football;
using Vortex.Rooms.Games.Football.Physics;
using Xunit;

namespace Vortex.Rooms.Tests.Football;

/// <summary>
/// The ball's movement rules, over a hand-built grid with no room, no furniture and no packets. This
/// is where football's edge cases are actually pinned: the room boundary, a solid tile, a player in
/// the way, the ordering that decides whether a ball goes into a goal or stops dead in front of it,
/// the net's mouth, and the bounce.
/// </summary>
public sealed class BallPhysicsTests
{
    private const int Width = 10;

    [Fact]
    public void AnOpenTileAhead_Rolls()
    {
        Grid grid = new();

        BallStep step = BallPhysics.Advance(Idx(5, 5), Rotation.East, grid);

        step.Outcome.Should().Be(BallStepOutcome.Rolled);
        step.TileIdx.Should().Be(Idx(6, 5));
    }

    [Fact]
    public void TheRoomEdge_Blocks_AndLeavesTheBallWhereItWas()
    {
        Grid grid = new();

        BallStep step = BallPhysics.Advance(Idx(9, 5), Rotation.East, grid);

        step.Outcome.Should().Be(BallStepOutcome.Blocked);
        step.TileIdx.Should().Be(Idx(9, 5));
    }

    [Fact]
    public void ASolidTile_Blocks()
    {
        Grid grid = new();
        grid.Closed.Add(Idx(6, 5));

        BallPhysics
            .Advance(Idx(5, 5), Rotation.East, grid)
            .Outcome.Should()
            .Be(BallStepOutcome.Blocked);
    }

    [Fact]
    public void APlayerWhoIntercepts_TacklesRatherThanBlocks()
    {
        // Two different endings: a wall turns the ball, a player takes it. Reporting them apart is
        // what stops a ball ricocheting off somebody's feet.
        Grid grid = new() { AvatarsStop = true };
        grid.Avatars.Add(Idx(6, 5));

        BallStep step = BallPhysics.Advance(Idx(5, 5), Rotation.East, grid);

        step.Outcome.Should().Be(BallStepOutcome.Tackled);
        step.TileIdx.Should().Be(Idx(5, 5));
    }

    [Fact]
    public void APlayerWhoMissesIt_LetsTheBallThrough()
    {
        // A ball that could never pass anybody makes a crowded pitch unplayable, so the interception
        // is a roll, not a certainty — and when it fails the ball rolls onto their tile.
        Grid grid = new() { AvatarsStop = false };
        grid.Avatars.Add(Idx(6, 5));

        BallStep step = BallPhysics.Advance(Idx(5, 5), Rotation.East, grid);

        step.Outcome.Should().Be(BallStepOutcome.Rolled);
        step.TileIdx.Should().Be(Idx(6, 5));
    }

    [Fact]
    public void AWallDecidesBeforeAPlayerDoes()
    {
        // The interception is the one question with a random answer. Asking it about a step a wall
        // had already refused would burn entropy and stop a seeded match replaying identically.
        Grid grid = new() { AvatarsStop = true };
        grid.Closed.Add(Idx(6, 5));
        grid.Avatars.Add(Idx(6, 5));

        BallPhysics
            .Advance(Idx(5, 5), Rotation.East, grid)
            .Outcome.Should()
            .Be(BallStepOutcome.Blocked);
        grid.AvatarQuestions.Should().Be(0);
    }

    [Fact]
    public void AGoalMouth_IsEnteredEvenThoughAGoalIsSolid()
    {
        // A goal net is furniture a loose item could not otherwise stand on. Checking the goal
        // before openness is what stops the ball dying on the goal line.
        Grid grid = new();
        grid.Goals[Idx(6, 5)] = Rotation.West;
        grid.Closed.Add(Idx(6, 5));

        BallStep step = BallPhysics.Advance(Idx(5, 5), Rotation.East, grid);

        step.Outcome.Should().Be(BallStepOutcome.Goal);
        step.TileIdx.Should().Be(Idx(6, 5));
    }

    [Fact]
    public void TheBackOfANet_IsSolid()
    {
        // Rolling into the back of the goal is not a goal. Without this a ball that got behind the
        // net scored from the wrong side, which is the whole reason the goal carries a facing.
        Grid grid = new();
        grid.Goals[Idx(6, 5)] = Rotation.East;

        BallPhysics
            .Advance(Idx(5, 5), Rotation.East, grid)
            .Outcome.Should()
            .Be(BallStepOutcome.Blocked);
    }

    [Theory]
    // A north-facing net opens to the south: the three southward headings go in.
    [InlineData(Rotation.South, Rotation.North, true)]
    [InlineData(Rotation.SouthEast, Rotation.North, true)]
    [InlineData(Rotation.SouthWest, Rotation.North, true)]
    [InlineData(Rotation.East, Rotation.North, false)]
    [InlineData(Rotation.West, Rotation.North, false)]
    [InlineData(Rotation.North, Rotation.North, false)]
    // And the same three-wide mouth on every other quarter turn.
    [InlineData(Rotation.West, Rotation.East, true)]
    [InlineData(Rotation.North, Rotation.South, true)]
    [InlineData(Rotation.East, Rotation.West, true)]
    public void AGoalOnlyTakesABallEnteringItsMouth(
        Rotation ballDirection,
        Rotation goalFacing,
        bool accepted
    )
    {
        BallPhysics.GoalAccepts(ballDirection, goalFacing).Should().Be(accepted);
    }

    [Fact]
    public void AGoalOnAnOddRotation_IsPlayableRatherThanInert()
    {
        // Not a shape the catalogue produces. Treating it as open beats leaving a net nothing can
        // ever enter sitting in somebody's room.
        BallPhysics.GoalAccepts(Rotation.North, Rotation.NorthEast).Should().BeTrue();
    }

    [Fact]
    public void NoDirection_Blocks()
    {
        Grid grid = new();

        BallPhysics
            .Advance(Idx(5, 5), Rotation.None, grid)
            .Outcome.Should()
            .Be(BallStepOutcome.Blocked);
    }

    [Fact]
    public void ARollGoesTheWayItWasKicked_InEveryCardinalDirection()
    {
        Grid grid = new();

        BallPhysics.Advance(Idx(5, 5), Rotation.North, grid).TileIdx.Should().Be(Idx(5, 4));
        BallPhysics.Advance(Idx(5, 5), Rotation.South, grid).TileIdx.Should().Be(Idx(5, 6));
        BallPhysics.Advance(Idx(5, 5), Rotation.West, grid).TileIdx.Should().Be(Idx(4, 5));
        BallPhysics.Advance(Idx(5, 5), Rotation.East, grid).TileIdx.Should().Be(Idx(6, 5));
    }

    // ---- bounce -------------------------------------------------------------

    [Theory]
    [InlineData(Rotation.North, Rotation.South)]
    [InlineData(Rotation.South, Rotation.North)]
    [InlineData(Rotation.East, Rotation.West)]
    [InlineData(Rotation.West, Rotation.East)]
    public void ACardinalBounce_ComesBackTheWayItCame(Rotation hit, Rotation expected)
    {
        BallPhysics.Bounce(Idx(5, 5), hit, new Grid()).Should().Be(expected);
    }

    [Fact]
    public void ADiagonalBounce_TakesTheFirstFreeTurn()
    {
        // A ball hitting a wall diagonally slides along it rather than coming straight back, which is
        // what makes a corner playable instead of a trap.
        Grid grid = new();

        BallPhysics.Bounce(Idx(5, 5), Rotation.NorthEast, grid).Should().Be(Rotation.NorthWest);
    }

    [Fact]
    public void ADiagonalBounce_TakesTheSecondTurnWhenTheFirstIsBlocked()
    {
        Grid grid = new();
        grid.Closed.Add(Idx(4, 4)); // north-west of (5,5)

        BallPhysics.Bounce(Idx(5, 5), Rotation.NorthEast, grid).Should().Be(Rotation.SouthEast);
    }

    [Fact]
    public void ADiagonalBounce_ReversesWhenBothTurnsAreBlocked()
    {
        Grid grid = new();
        grid.Closed.Add(Idx(4, 4)); // north-west
        grid.Closed.Add(Idx(6, 6)); // south-east

        BallPhysics.Bounce(Idx(5, 5), Rotation.NorthEast, grid).Should().Be(Rotation.SouthWest);
    }

    [Fact]
    public void ABounceIntoAGoalMouth_CountsAsAFreeTurn()
    {
        // The turn is chosen by asking where the ball could go, and a net's mouth is somewhere it
        // can go even though the tile itself is solid.
        Grid grid = new();
        grid.Closed.Add(Idx(4, 4));
        grid.Goals[Idx(4, 4)] = Rotation.SouthEast;

        BallPhysics.Bounce(Idx(5, 5), Rotation.NorthEast, grid).Should().Be(Rotation.NorthWest);
    }

    // ---- cadence and roll state ---------------------------------------------

    [Fact]
    public void AFullKick_IsFastThenSlow()
    {
        FootballSettings s = FootballSettings.Default;

        // A struck ball has pace for its first few tiles and then slows; that acceleration curve is
        // the whole of what a viewer reads as "physics".
        BallPhysics.StepDelayMs(1, 6, s).Should().Be(s.FastStepMs);
        BallPhysics.StepDelayMs(4, 6, s).Should().Be(s.FastStepMs);
        BallPhysics.StepDelayMs(5, 6, s).Should().Be(s.SlowStepMs);
        BallPhysics.StepDelayMs(6, 6, s).Should().Be(s.SlowStepMs);
    }

    [Fact]
    public void ANudge_NeverPicksUpPace()
    {
        FootballSettings s = FootballSettings.Default;

        // A one-tile dribble must not flick across the floor faster than a kick's first tile.
        BallPhysics.StepDelayMs(1, 1, s).Should().Be(s.SlowStepMs);
        BallPhysics.StepDelayMs(1, s.FastSteps, s).Should().Be(s.SlowStepMs);
    }

    [Fact]
    public void TheRollState_CountsDownAndClearsAtRest()
    {
        // The furni has no idea it is a football: the roll animation is entirely this number, and a
        // ball left holding a non-zero one spins on the floor forever.
        BallPhysics.RollState(6).Should().Be(7);
        BallPhysics.RollState(1).Should().Be(2);
        BallPhysics.RollState(0).Should().Be(FootballConstants.BallRestingState);
        BallPhysics.RollState(-1).Should().Be(FootballConstants.BallRestingState);
    }

    private static int Idx(int x, int y) => (y * Width) + x;

    /// <summary>A 10x10 pitch: everything open unless a test says otherwise.</summary>
    private sealed class Grid : IBallSpace
    {
        public HashSet<int> Closed { get; } = [];

        public HashSet<int> Avatars { get; } = [];

        public Dictionary<int, Rotation> Goals { get; } = [];

        /// <summary>Stands in for the room's roll, so a test decides the interception rather than
        /// racing it.</summary>
        public bool AvatarsStop { get; init; }

        public int AvatarQuestions { get; private set; }

        public bool TryStep(int fromTileIdx, Rotation direction, out int nextTileIdx)
        {
            int x = fromTileIdx % Width;
            int y = fromTileIdx / Width;

            (int dx, int dy) = direction switch
            {
                Rotation.North => (0, -1),
                Rotation.NorthEast => (1, -1),
                Rotation.East => (1, 0),
                Rotation.SouthEast => (1, 1),
                Rotation.South => (0, 1),
                Rotation.SouthWest => (-1, 1),
                Rotation.West => (-1, 0),
                Rotation.NorthWest => (-1, -1),
                _ => (0, 0),
            };

            int nx = x + dx;
            int ny = y + dy;

            if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= Width || ny >= Width)
            {
                nextTileIdx = -1;

                return false;
            }

            nextTileIdx = (ny * Width) + nx;

            return true;
        }

        public bool IsOpen(int tileIdx) => !Closed.Contains(tileIdx);

        public bool AvatarStopsBall(int tileIdx)
        {
            if (!Avatars.Contains(tileIdx))
            {
                return false;
            }

            AvatarQuestions++;

            return AvatarsStop;
        }

        public bool TryGetGoal(int tileIdx, out Rotation facing) =>
            Goals.TryGetValue(tileIdx, out facing);
    }
}
