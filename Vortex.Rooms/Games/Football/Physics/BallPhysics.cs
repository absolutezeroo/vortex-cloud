using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Rooms.Games.Football.Physics;

/// <summary>What one step of a ball's travel did.</summary>
public enum BallStepOutcome
{
    /// <summary>The ball moved onto <c>TileIdx</c> and is still travelling.</summary>
    Rolled = 0,

    /// <summary>Nothing there will take the ball: the room edge, a solid tile, a stack it cannot
    /// climb, or the back of a net. The ball stays put and the caller may bounce it.</summary>
    Blocked = 1,

    /// <summary>It entered a goal on <c>TileIdx</c>.</summary>
    Goal = 2,

    /// <summary>A player got in the way and took it. The ball stays put and does NOT bounce — it was
    /// stopped, not deflected.</summary>
    Tackled = 3,
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

    /// <summary>Whether somebody standing there takes the ball off it. The room rolls for this, so
    /// it is a question and not a fact: a player does not stop every ball that reaches them.</summary>
    bool AvatarStopsBall(int tileIdx);

    /// <summary>The net on that tile and the way it faces, if there is one.</summary>
    bool TryGetGoal(int tileIdx, out Rotation facing);
}

/// <summary>
/// The ball's movement rules, and nothing else — no scores, no teams, no packets, no room. Football's
/// rules decide what a goal is worth and when a match ends; this decides only where a rolling ball
/// ends up next, which is why the two are separate files.
/// <para>
/// The server is authoritative throughout: a client never says where the ball is, only that a player
/// walked into it, and every tile the ball occupies is one this function chose.
/// </para>
/// <para>
/// <b>Where the numbers come from.</b> Habbo's own football is undocumented — no capture exists and
/// the official client carries no football logic, because a <c>fball</c> is an ordinary floor item
/// the server slides. The behaviour modelled here (bounce off what blocks it, a net that only accepts
/// a ball entering its mouth, a player who usually but not always intercepts) is what the open-source
/// reference emulator does. Per the repository contract that is <b>evidence, not authority</b>: it is
/// the best-attested description of the game available, and every number it implies is admin-editable
/// in <see cref="FootballSettings"/> rather than compiled in as though it were wire-fixed.
/// </para>
/// </summary>
public static class BallPhysics
{
    /// <summary>
    /// Advances a ball one tile.
    /// <para>
    /// A goal is checked before openness, because a net is normally a solid furni a loose item could
    /// not otherwise stand on — but only from the front: <see cref="GoalAccepts"/> rejects a ball
    /// arriving at the back or the side, which then blocks and bounces like any other obstacle.
    /// </para>
    /// </summary>
    public static BallStep Advance(int fromTileIdx, Rotation direction, IBallSpace space)
    {
        if (direction == Rotation.None || !space.TryStep(fromTileIdx, direction, out int nextIdx))
        {
            // The room's edge.
            return new BallStep(BallStepOutcome.Blocked, fromTileIdx);
        }

        if (space.TryGetGoal(nextIdx, out Rotation facing))
        {
            return GoalAccepts(direction, facing)
                ? new BallStep(BallStepOutcome.Goal, nextIdx)
                : new BallStep(BallStepOutcome.Blocked, fromTileIdx);
        }

        if (!space.IsOpen(nextIdx))
        {
            return new BallStep(BallStepOutcome.Blocked, fromTileIdx);
        }

        // Asked last, and only about a tile the ball could otherwise have reached, because it is the
        // one question with a random answer: rolling for it earlier would burn entropy on a step that
        // a wall had already decided, and a seeded match would stop replaying identically.
        return space.AvatarStopsBall(nextIdx)
            ? new BallStep(BallStepOutcome.Tackled, fromTileIdx)
            : new BallStep(BallStepOutcome.Rolled, nextIdx);
    }

    /// <summary>
    /// Whether a ball travelling <paramref name="ballDirection"/> is entering the mouth of a net
    /// facing <paramref name="goalFacing"/>. The mouth spans the three directions opposite the net's
    /// facing — a north-facing goal takes a ball heading south, south-east or south-west — so the
    /// back and both sides are solid, which is what stops a ball rolling behind the net from scoring.
    /// </summary>
    public static bool GoalAccepts(Rotation ballDirection, Rotation goalFacing)
    {
        if (ballDirection == Rotation.None)
        {
            return false;
        }

        // A goal on an odd rotation is not a shape the catalogue produces; treated as open rather
        // than as a net nothing can ever enter, so a mis-rotated furni is playable instead of inert.
        if (goalFacing == Rotation.None || ((int)goalFacing & 1) == 1)
        {
            return true;
        }

        int delta = (((int)ballDirection - (int)goalFacing) + 8) % 8;

        return delta is 3 or 4 or 5;
    }

    /// <summary>
    /// The direction a ball takes when something stopped it. A cardinal simply comes back the way it
    /// came; a diagonal tries the two directions that keep it moving along the wall it hit, in a
    /// fixed order, and reverses only when neither is free. Returns <see cref="Rotation.None"/> when
    /// the ball cannot go anywhere, which is the caller's signal to let it rest.
    /// </summary>
    public static Rotation Bounce(int fromTileIdx, Rotation direction, IBallSpace space)
    {
        switch (direction)
        {
            case Rotation.North:
                return Rotation.South;
            case Rotation.South:
                return Rotation.North;
            case Rotation.East:
                return Rotation.West;
            case Rotation.West:
                return Rotation.East;

            case Rotation.NorthEast:
                return FirstOpen(fromTileIdx, space, Rotation.NorthWest, Rotation.SouthEast)
                    ?? Rotation.SouthWest;
            case Rotation.SouthEast:
                return FirstOpen(fromTileIdx, space, Rotation.SouthWest, Rotation.NorthEast)
                    ?? Rotation.NorthWest;
            case Rotation.SouthWest:
                return FirstOpen(fromTileIdx, space, Rotation.SouthEast, Rotation.NorthWest)
                    ?? Rotation.NorthEast;
            case Rotation.NorthWest:
                return FirstOpen(fromTileIdx, space, Rotation.NorthEast, Rotation.SouthWest)
                    ?? Rotation.SouthEast;

            default:
                return Rotation.None;
        }
    }

    /// <summary>
    /// How long before the ball's next hop. A kick is fast for its first few tiles and then slows,
    /// which is the whole of the ball's "physics" as far as a viewer is concerned; a nudge of one or
    /// two tiles never reaches the fast phase and is slow throughout.
    /// </summary>
    /// <param name="currentStep">1-based, counting the hop about to be taken.</param>
    public static int StepDelayMs(int currentStep, int totalSteps, FootballSettings settings) =>
        totalSteps > settings.FastSteps && currentStep <= settings.FastSteps
            ? settings.FastStepMs
            : settings.SlowStepMs;

    /// <summary>
    /// The value a rolling ball shows. It counts down to 1 over the kick and is cleared to
    /// <see cref="FootballConstants.BallRestingState"/> when the ball stops, which is how the client
    /// animates it: the furni has no idea it is a football, so the roll is entirely in this number.
    /// </summary>
    public static int RollState(int stepsRemaining) =>
        stepsRemaining <= 0 ? FootballConstants.BallRestingState : stepsRemaining + 1;

    private static Rotation? FirstOpen(
        int fromTileIdx,
        IBallSpace space,
        Rotation first,
        Rotation second
    )
    {
        if (CanEnter(fromTileIdx, first, space))
        {
            return first;
        }

        return CanEnter(fromTileIdx, second, space) ? second : null;
    }

    private static bool CanEnter(int fromTileIdx, Rotation direction, IBallSpace space)
    {
        if (!space.TryStep(fromTileIdx, direction, out int nextIdx))
        {
            return false;
        }

        return space.TryGetGoal(nextIdx, out Rotation facing)
            ? GoalAccepts(direction, facing)
            : space.IsOpen(nextIdx);
    }
}
