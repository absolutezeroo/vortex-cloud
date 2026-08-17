using System.Collections.Generic;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// The outcome of a finished round, built from the shared team state the moment GAME_ENDS fires —
/// while the final scores are still standing and the participants are still in their teams. This is
/// what the high-score boards record; membership read any later would already be shrinking as
/// players leave.
/// </summary>
public sealed record GameRoundResult
{
    /// <summary>The leading team, or <see cref="GameTeamColor.None"/> on a scoreless round.</summary>
    public required GameTeamColor WinningTeam { get; init; }

    /// <summary>Final score per real team (Red..Yellow), zeros included.</summary>
    public required IReadOnlyDictionary<GameTeamColor, int> Scores { get; init; }

    /// <summary>The display names of each team's members at round end. Players who left mid-round
    /// left their team with it, so they are deliberately absent — membership semantics, not a bug.</summary>
    public required IReadOnlyDictionary<
        GameTeamColor,
        IReadOnlyList<string>
    > MemberNames { get; init; }
}
