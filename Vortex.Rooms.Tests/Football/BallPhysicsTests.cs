using System.Collections.Generic;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Rooms.Games.Football.Physics;
using Xunit;

namespace Vortex.Rooms.Tests.Football;

/// <summary>
/// The ball's movement rules, over a hand-built grid with no room, no furniture and no packets. This
/// is where football's edge cases are actually pinned: the room boundary, a solid tile, a player in
/// the way, and the ordering that decides whether a ball goes into a goal or stops dead in front of
/// it.
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
    public void APlayerInTheWay_Blocks()
    {
        Grid grid = new();
        grid.Avatars.Add(Idx(6, 5));

        BallPhysics
            .Advance(Idx(5, 5), Rotation.East, grid)
            .Outcome.Should()
            .Be(BallStepOutcome.Blocked);
    }

    [Fact]
    public void AGoal_IsEnteredEvenThoughAGoalMouthIsSolid()
    {
        // A goal net is furniture a loose item could not otherwise stand on. Checking the goal
        // before openness is what stops the ball dying on the goal line.
        Grid grid = new();
        grid.Goals.Add(Idx(6, 5));
        grid.Closed.Add(Idx(6, 5));

        BallStep step = BallPhysics.Advance(Idx(5, 5), Rotation.East, grid);

        step.Outcome.Should().Be(BallStepOutcome.Goal);
        step.TileIdx.Should().Be(Idx(6, 5));
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

    private static int Idx(int x, int y) => (y * Width) + x;

    /// <summary>A 10x10 pitch: everything open unless a test says otherwise.</summary>
    private sealed class Grid : IBallSpace
    {
        public HashSet<int> Closed { get; } = [];

        public HashSet<int> Avatars { get; } = [];

        public HashSet<int> Goals { get; } = [];

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

        public bool HasAvatar(int tileIdx) => Avatars.Contains(tileIdx);

        public bool IsGoal(int tileIdx) => Goals.Contains(tileIdx);
    }
}
