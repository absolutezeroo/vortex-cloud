using System.Collections.Generic;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Rooms.Games.Presentation;

/// <summary>
/// A finished round, in the shape a Habbo high-score board can record: keyed by colour, because
/// <c>highscore_*</c> furni store per-colour rows and have no notion of any other kind of team.
/// <para>
/// It is deliberately NOT what a match produces. A match produces a <c>MatchOutcome</c> in the game's
/// own teams; this is the presentation layer's projection of one, and keeping the two apart is what
/// lets a game with teams the four colours cannot express still finish cleanly — it simply has
/// nothing to write on a coloured board.
/// </para>
/// </summary>
public sealed record GameMatchResult
{
    /// <summary>The leading team's colour, or <see cref="GameTeamColor.None"/> on a scoreless round.</summary>
    public required GameTeamColor WinningTeam { get; init; }

    /// <summary>Final score per colour, zeros included.</summary>
    public required IReadOnlyDictionary<GameTeamColor, int> Scores { get; init; }

    /// <summary>The display names of each colour's members at the final whistle.</summary>
    public required IReadOnlyDictionary<
        GameTeamColor,
        IReadOnlyList<string>
    > MemberNames { get; init; }
}
