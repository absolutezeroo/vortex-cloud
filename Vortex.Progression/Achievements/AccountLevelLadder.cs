using System.Collections.Generic;
using System.Linq;

namespace Vortex.Players.Achievements;

/// <summary>
/// Turns an achievement score into the level the profile shows. Pure, because the alternative — the
/// constant 1 this replaces — is exactly the bug it exists to prevent, and the boundary behaviour
/// (a threshold counts as reached) is the sort of thing that only shows up as "my level never went
/// up" days later.
/// </summary>
public static class AccountLevelLadder
{
    /// <summary>The level every account has before earning anything. The client shows it verbatim,
    /// so zero would read as "Level 0" rather than "new".</summary>
    public const int FloorLevel = 1;

    /// <summary>
    /// The highest level whose required score the player has reached, or <see cref="FloorLevel"/>
    /// when the ladder is empty or they are below the first rung.
    /// </summary>
    /// <param name="rungs">(level, required score) pairs, in any order.</param>
    /// <param name="achievementScore">The player's score; negatives are treated as zero.</param>
    public static int Resolve(
        IEnumerable<(int Level, int RequiredScore)> rungs,
        int achievementScore
    )
    {
        int score = achievementScore < 0 ? 0 : achievementScore;
        int level = FloorLevel;

        // Ordered by score so a ladder entered out of order still resolves, and the highest reached
        // rung wins rather than the first one that happens to match.
        foreach ((int rungLevel, int requiredScore) in rungs.OrderBy(r => r.RequiredScore))
        {
            if (score < requiredScore)
            {
                break;
            }

            if (rungLevel > level)
            {
                level = rungLevel;
            }
        }

        return level;
    }
}
