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

    /// <summary>The travel the kick has left, which is also its pace: the ball slows one rung per
    /// tile, so what remains and how fast it is going are the same number.</summary>
    public int StepsRemaining { get; set; }

    /// <summary>Whether hitting something turns the ball rather than stopping it. A struck ball
    /// bounces; a ball being dribbled along by a walking player does not.</summary>
    public bool CanBounce { get; set; }

    public long NextStepAtMs { get; set; }

    /// <summary>Who kicked it. Carried onto the goal event so an own goal stays distinguishable.</summary>
    public PlayerId LastKicker { get; set; }

    /// <summary>Where the ball sat when the match was prepared; it returns here after a goal. -1
    /// outside a match, where there is nothing to return to.</summary>
    public int KickoffTileIdx { get; set; } = -1;

    /// <summary>Set while the ball is sitting in a goal waiting to be put back.</summary>
    public long ReturnAtMs { get; set; }

    /// <summary>Who is dribbling this ball and where they were walking to when they last nudged it.
    /// <para>
    /// A nudge pushes the ball onto the very tile the player is walking to, so one step later
    /// "their destination is the ball's tile" — the test for a deliberate strike — is true for a
    /// ball they merely bumped into. Every walk that met the ball therefore ended in a full-power
    /// shot on its last step, and the dribble, which the game does implement, could not be reached
    /// at all. Remembering the walk is what tells the two apart.
    /// </para></summary>
    public PlayerId Dribbler { get; private set; }

    public int DribbleGoalTileIdx { get; private set; } = -1;

    public bool IsRolling => StepsRemaining > 0 && Direction != Rotation.None;

    public bool IsWaitingToReturn => ReturnAtMs > 0;

    public bool IsIdle => !IsRolling && !IsWaitingToReturn;

    public void Kick(
        MatchId match,
        PlayerId kicker,
        Rotation direction,
        int steps,
        bool canBounce,
        long startAtMs
    )
    {
        // A ball already on its way to a goal reset is not kickable; the caller checks. A second kick
        // while rolling simply replaces the first — the room's turn serialises them, so "two players
        // kicked at once" is really "one then the other", and the later one wins.
        Match = match;
        LastKicker = kicker;
        Direction = direction;
        StepsRemaining = steps;
        CanBounce = canBounce;
        NextStepAtMs = startAtMs;
        ReturnAtMs = 0;
    }

    /// <summary>Whether this contact is the continuation of a dribble rather than a fresh aim at the
    /// ball: the same player, still walking to the same tile, having already pushed it there.</summary>
    public bool IsDribbleInProgress(PlayerId playerId, int goalTileIdx) =>
        DribbleGoalTileIdx == goalTileIdx && Dribbler == playerId;

    public void Dribbled(PlayerId playerId, int goalTileIdx)
    {
        Dribbler = playerId;
        DribbleGoalTileIdx = goalTileIdx;
    }

    /// <summary>Deliberately NOT called from <see cref="Stop"/>: a nudged ball rests after its one
    /// tile, and forgetting the dribble there would make the walker's very next step a strike —
    /// which is the bug this exists to prevent.</summary>
    public void ForgetDribble()
    {
        Dribbler = default;
        DribbleGoalTileIdx = -1;
    }

    public void Stop()
    {
        Direction = Rotation.None;
        StepsRemaining = 0;
        CanBounce = false;
        NextStepAtMs = 0;
        ReturnAtMs = 0;
    }
}
