using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Games;

namespace Vortex.Rooms.Games.Football.Physics;

/// <summary>
/// Where one ball is going, how much travel it has left, and who set it going. Held by the game, not
/// by the ball's furni: a ball cannot keep rolling after the match that kicked it ended, and the way
/// to guarantee that is for the motion to live in the match's own state and be dropped with it.
/// </summary>
public sealed class BallMotion
{
    /// <summary>The match the current kick belongs to, or <see cref="MatchId.None"/> for a ball
    /// kicked outside one — a football is kickable in any room, with or without a game.</summary>
    public MatchId Match { get; set; }

    public Rotation Direction { get; set; } = Rotation.None;

    public int StepsRemaining { get; set; }

    public long NextStepAtMs { get; set; }

    /// <summary>Who kicked it. Carried onto the goal event so an own goal stays distinguishable.</summary>
    public PlayerId LastKicker { get; set; }

    /// <summary>Where the ball sat when the match was prepared; it returns here after a goal. -1
    /// outside a match, where there is nothing to return to.</summary>
    public int KickoffTileIdx { get; set; } = -1;

    /// <summary>Set while the ball is sitting in a goal waiting to be put back.</summary>
    public long ReturnAtMs { get; set; }

    public bool IsRolling => StepsRemaining > 0 && Direction != Rotation.None;

    public bool IsWaitingToReturn => ReturnAtMs > 0;

    public bool IsIdle => !IsRolling && !IsWaitingToReturn;

    public void Kick(MatchId match, PlayerId kicker, Rotation direction, int steps, long startAtMs)
    {
        // A ball already on its way to a goal reset is not kickable; the caller checks. A second kick
        // while rolling simply replaces the first — the room's turn serialises them, so "two players
        // kicked at once" is really "one then the other", and the later one wins.
        Match = match;
        LastKicker = kicker;
        Direction = direction;
        StepsRemaining = steps;
        NextStepAtMs = startAtMs;
        ReturnAtMs = 0;
    }

    public void Stop()
    {
        Direction = Rotation.None;
        StepsRemaining = 0;
        NextStepAtMs = 0;
        ReturnAtMs = 0;
    }
}
