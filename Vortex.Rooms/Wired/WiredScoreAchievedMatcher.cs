namespace Vortex.Rooms.Wired;

/// <summary>
/// Pure fire condition for the wired SCORE_ACHIEVED trigger, mirroring the client's team + score
/// selector (ScoreAchieved.ts). It fires exactly once — on the tick a team's score crosses up onto
/// the configured threshold — for either a specific team or any team.
/// </summary>
public static class WiredScoreAchievedMatcher
{
    /// <summary>
    /// Whether a score change should fire the trigger. <paramref name="configTeam"/> 0 = any team;
    /// 1-4 = a specific team id (GameTeamColor). It fires only when the score was below
    /// <paramref name="configScore"/> and is now at or above it, so a single crossing fires once even
    /// as points keep accruing.
    /// </summary>
    public static bool Matches(
        int configTeam,
        int configScore,
        int eventTeam,
        int eventScore,
        int eventPreviousScore
    )
    {
        if (configTeam != 0 && eventTeam != configTeam)
        {
            return false;
        }

        return eventPreviousScore < configScore && eventScore >= configScore;
    }
}
