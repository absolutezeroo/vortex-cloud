namespace Vortex.Rooms.Games.Football;

/// <summary>
/// The tunable balance of a football match, resolved once per match from server config by
/// <see cref="FootballConfig.ResolveAsync"/>.
/// <para>
/// Every value here is a Vortex choice, not a Habbo one. Habbo's own numbers for a football kick are
/// <b>unknown</b>: no capture of the official server exists, the client contains no football logic
/// to read them from, and the reference emulators disagree. The defaults below are what plays well
/// and what the reference implementations cluster around — they are explicitly assumptions, which is
/// why they are all admin-editable rather than compiled in beside the genuinely wire-fixed values in
/// <see cref="FootballConstants"/>.
/// </para>
/// </summary>
public sealed record FootballSettings
{
    public static readonly FootballSettings Default = new();

    /// <summary>How many tiles a kick carries the ball.</summary>
    public int KickDistance { get; init; } = 6;

    /// <summary>Milliseconds between the ball's tile hops. One room tick is 50 ms, so this is
    /// rounded up to the next tick boundary in practice.</summary>
    public int BallStepMs { get; init; } = 200;

    /// <summary>Points a goal is worth to the team whose colour the goal carries.</summary>
    public int GoalPoints { get; init; } = 1;

    /// <summary>How long the ball sits in the goal before returning to the kickoff spot.</summary>
    public int GoalResetMs { get; init; } = 2_000;

    public int MaxPlayersPerTeam { get; init; } = 5;
}
