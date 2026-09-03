using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Rooms.Games.Football.Physics;

/// <summary>What one step of a ball's travel did.</summary>
public enum BallStepOutcome
{
    /// <summary>The ball moved onto <c>TileIdx</c> and is still travelling.</summary>
    Rolled = 0,

    /// <summary>Something stopped it: the room edge, a solid tile, a stack it cannot climb, or a
    /// player standing in the way. The ball stays where it was.</summary>
    Blocked = 1,

    /// <summary>It entered a goal on <c>TileIdx</c>.</summary>
    Goal = 2,
}

/// <summary>The outcome of one step, and the tile it applies to.</summary>
public readonly record struct BallStep(BallStepOutcome Outcome, int TileIdx);

/// <summary>
/// The space a ball travels through, as the physics needs to see it. The point of the interface is
/// that the ball simulation is a pure function of the grid: it can be driven over a hand-built test
/// grid with no room, no furniture and no packets, which is where every edge case in
/// <see cref="BallPhysics"/> is actually pinned down.
/// </summary>
public interface IBallSpace
{
    /// <summary>The tile one step from here in that direction; false at the room's edge.</summary>
    bool TryStep(int fromTileIdx, Rotation direction, out int nextTileIdx);

    /// <summary>Whether a floor item may occupy that tile at all.</summary>
    bool IsOpen(int tileIdx);

    /// <summary>Whether somebody is standing there.</summary>
    bool HasAvatar(int tileIdx);

    /// <summary>Whether that tile is a goal mouth.</summary>
    bool IsGoal(int tileIdx);
}

/// <summary>
/// The ball's movement rules, and nothing else — no scores, no teams, no packets, no room. Football's
/// rules decide what a goal is worth and when a match ends; this decides only where a rolling ball
/// ends up next, which is why the two are separate files.
/// <para>
/// The server is authoritative throughout: a client never says where the ball is, only that a player
/// walked into it, and every tile the ball occupies is one this function chose.
/// </para>
/// </summary>
public static class BallPhysics
{
    /// <summary>
    /// Advances a ball one tile. A goal is checked BEFORE openness, because a goal mouth is
    /// frequently a solid furni that a loose item could not otherwise stand on — a ball that stopped
    /// dead in front of the net instead of going in was the first thing this ordering fixed.
    /// </summary>
    public static BallStep Advance(int fromTileIdx, Rotation direction, IBallSpace space)
    {
        if (direction == Rotation.None || !space.TryStep(fromTileIdx, direction, out int nextIdx))
        {
            // The room's edge.
            return new BallStep(BallStepOutcome.Blocked, fromTileIdx);
        }

        if (space.IsGoal(nextIdx))
        {
            return new BallStep(BallStepOutcome.Goal, nextIdx);
        }

        if (!space.IsOpen(nextIdx) || space.HasAvatar(nextIdx))
        {
            return new BallStep(BallStepOutcome.Blocked, fromTileIdx);
        }

        return new BallStep(BallStepOutcome.Rolled, nextIdx);
    }
}
