using Vortex.Primitives.Rooms.Games;

namespace Vortex.Rooms.Games.Football;

/// <summary>
/// Football's wire-fixed values. There are few, because the client has no football RULES: a
/// <c>fball</c> is a pushable floor item the server slides, and where it goes is decided here.
/// What the client does have is <c>FurniturePushableLogic</c>, which reads the ball's state — so
/// that encoding is wire-fixed and lives below. Everything else — how far a kick travels, how fast
/// the ball rolls, what a goal is worth — is balance, and lives in <see cref="FootballConfig"/>.
/// </summary>
public static class FootballConstants
{
    /// <summary>The game's identity. Every football component carries it and the runtime routes on it.</summary>
    public static readonly GameId Game = new("football");

    /// <summary>The ball's state at rest — the client's <c>ANIMATION_NOT_MOVING</c>, and the value
    /// that also tells it to stop interpolating. See <c>BallPhysics.RollState</c>.</summary>
    public const int BallRestingState = 0;

    /// <summary>The client's <c>ANIMATION_MOVING</c>: the units digit of a rolling ball's state.</summary>
    public const int BallMovingAnimation = 1;

    /// <summary>The interval <c>MovingObjectLogic</c> slides an object over when nothing tells it
    /// otherwise. The ball's state divides it — see <c>BallPhysics.RollState</c> — which is the only
    /// way the server has of saying "play this hop in 125ms, not half a second".</summary>
    public const int PushableAnimationTimeMs = 500;

    /// <summary>The goal furni's state while a ball is in it. One state, held for the celebration and
    /// cleared when the ball returns to the spot — the goals are plain multistate furni.</summary>
    public const int GoalScoredState = 1;

    public const int GoalIdleState = 0;
}
