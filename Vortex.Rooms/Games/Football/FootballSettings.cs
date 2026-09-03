namespace Vortex.Rooms.Games.Football;

/// <summary>
/// The tunable balance of a football match, resolved once per match from server config by
/// <see cref="FootballConfig.ResolveAsync"/>.
/// <para>
/// <b>Where these numbers come from.</b> Habbo's own values are <b>not authoritatively known</b>: no
/// capture of the official server exists, and the client contains no football logic to read them
/// from — a <c>fball</c> is an ordinary floor item the server slides. They are, however, not
/// invented: every default below is the value the open-source reference emulator uses, which the
/// repository contract treats as <b>evidence, not authority</b>. That is why they are all
/// admin-editable rather than compiled in beside the genuinely wire-fixed values in
/// <see cref="FootballConstants"/> — an operator with better evidence can correct any of them
/// without a build.
/// </para>
/// </summary>
public sealed record FootballSettings
{
    public static readonly FootballSettings Default = new();

    /// <summary>How many tiles a deliberate kick carries the ball — the player walked at the ball's
    /// own tile rather than through it.</summary>
    public int KickDistance { get; init; } = 6;

    /// <summary>How far the ball goes when a player walks THROUGH its tile on the way somewhere else.
    /// One tile: they dribbled it along rather than struck it, and a dribble does not bounce.</summary>
    public int DragDistance { get; init; } = 1;

    /// <summary>How far the ball goes when a player clicks it from an adjacent tile without stepping
    /// on it — shorter than a run-up kick.</summary>
    public int TackleDistance { get; init; } = 4;

    /// <summary>Milliseconds between the ball's first hops, while the kick still has pace.</summary>
    public int FastStepMs { get; init; } = 125;

    /// <summary>Milliseconds between hops once the ball is slowing, and for a kick too short to ever
    /// pick up pace.</summary>
    public int SlowStepMs { get; init; } = 500;

    /// <summary>How many hops are taken at <see cref="FastStepMs"/>. A kick shorter than this is slow
    /// throughout, so a one-tile dribble does not flick across the floor.</summary>
    public int FastSteps { get; init; } = 4;

    /// <summary>The chance, in percent, that a player standing in the ball's path actually stops it.
    /// Not 100: a ball that could never pass anybody makes a crowded pitch unplayable, and the
    /// reference emulator rolls for it too.</summary>
    public int AvatarStopChancePercent { get; init; } = 70;

    /// <summary>Points a goal is worth to the team whose colour the goal carries.</summary>
    public int GoalPoints { get; init; } = 1;

    /// <summary>How long the ball sits in the goal before returning to the kickoff spot. <b>0 leaves
    /// it in the net</b>, which is what the reference emulator does — the return is a Vortex
    /// addition, kept on by default because a match with one ball otherwise stalls until somebody
    /// walks it out.</summary>
    public int GoalResetMs { get; init; } = 2_000;

    public int MaxPlayersPerTeam { get; init; } = 5;
}
