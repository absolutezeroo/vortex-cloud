using Vortex.Primitives.Rooms.Games;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.Abstractions;

/// <summary>
/// The static shape of a game, declared once by its module and read by the runtime. It is what lets
/// the runtime own the lifecycle without knowing what the game is: how many rounds to play, whether
/// to hold a countdown, how long to dwell on the result, and whether the game needs a tick at all
/// while nothing is happening.
/// <para>
/// Everything here is structure, not balance. Points, durations and capacities that an operator
/// should be able to change live come from server config, resolved by the module at match start.
/// </para>
/// </summary>
public sealed record GameProfile
{
    public required GameId Id { get; init; }

    /// <summary>Which team colours this game plays with, and how many fit in one.</summary>
    public TeamLayout Teams { get; init; } = TeamLayout.FourColours;

    /// <summary>Rounds per match. 1 for every Habbo room game shipped today; the loop back from
    /// <see cref="GamePhase.RoundEnding"/> to <see cref="GamePhase.Preparing"/> exists so a game that
    /// wants best-of-three does not have to re-implement the lifecycle.</summary>
    public int Rounds { get; init; } = 1;

    /// <summary>How long <see cref="GamePhase.Countdown"/> lasts. 0 skips the phase entirely, which
    /// is what the Habbo games do: the timer furni's button IS the countdown.</summary>
    public int CountdownMs { get; init; }

    /// <summary>How long the game dwells in <see cref="GamePhase.RoundEnding"/> showing the outcome
    /// before the match is finished and reset. Banzai's winner flicker needs it; a game with nothing
    /// to show leaves it at 0 and ends in the same turn.</summary>
    public int RoundEndMs { get; init; }
}
