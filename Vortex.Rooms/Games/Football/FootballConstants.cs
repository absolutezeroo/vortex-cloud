using Vortex.Primitives.Rooms.Games;

namespace Vortex.Rooms.Games.Football;

/// <summary>
/// Football's wire-fixed values. There are very few, because the client has no football logic at
/// all: a <c>fball</c> is an ordinary floor item, and the whole game is the server moving it with
/// the same slide the rollers use. Everything else — how far a kick travels, how fast the ball
/// rolls, what a goal is worth — is balance, and lives in <see cref="FootballConfig"/>.
/// </summary>
public static class FootballConstants
{
    /// <summary>The game's identity. Every football component carries it and the runtime routes on it.</summary>
    public static readonly GameId Game = new("football");

    /// <summary>The ball's state at rest. While it rolls the state counts the hops it has left, which
    /// is what the client animates from; see <c>BallPhysics.RollState</c>.</summary>
    public const int BallRestingState = 0;

    /// <summary>The goal furni's state while a ball is in it. One state, held for the celebration and
    /// cleared when the ball returns to the spot — the goals are plain multistate furni.</summary>
    public const int GoalScoredState = 1;

    public const int GoalIdleState = 0;
}
