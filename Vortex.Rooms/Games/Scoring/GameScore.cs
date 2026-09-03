using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.Scoring;

/// <summary>
/// Why a score changed. A string rather than an enum for the same reason <c>GameId</c> is: a game
/// contributed by a plugin has its own vocabulary of scoring acts and cannot add a member to a core
/// enum. Keep values short, lowercase and stable — they end up in logs and, later, in whatever reads
/// score events for quests and analytics.
/// </summary>
public readonly record struct ScoreReason(string Value)
{
    /// <summary>A wired give-score box, inside or outside a match.</summary>
    public static readonly ScoreReason Wired = new("wired");

    /// <summary>A score with no stated cause — the fallback, and a smell if it shows up in a game.</summary>
    public static readonly ScoreReason Unspecified = new("unspecified");

    public override string ToString() => Value;
}

/// <summary>
/// One scoring act, with its context. The point of carrying the player, the reason and the furni
/// that caused it is that <c>team.Score += 5</c> tells nobody anything afterwards: with this, a log
/// line reconstructs a match, an own goal stays distinguishable from a goal, and the quest,
/// achievement and analytics work that comes later has something to subscribe to instead of a diff
/// between two integers.
/// </summary>
/// <param name="Team">The team credited, in the GAME's own terms — never a colour. A team the
/// scoring book does not know (<see cref="TeamId.None"/> included) is a no-op, not an error: a
/// teamless player triggering a scoring act simply scores nothing.</param>
/// <param name="Player">Who caused it, where that is meaningful. Default when nobody did.</param>
/// <param name="Amount">Points, positive or negative. The team total floors at zero.</param>
/// <param name="Reason">The scoring act.</param>
/// <param name="Source">The furni that caused it, where there is one.</param>
public readonly record struct GameScore(
    TeamId Team,
    PlayerId Player,
    int Amount,
    ScoreReason Reason,
    RoomObjectId Source
)
{
    public static GameScore For(TeamId team, int amount, ScoreReason reason) =>
        new(team, default, amount, reason, default);

    public static GameScore By(
        TeamId team,
        PlayerId player,
        int amount,
        ScoreReason reason
    ) => new(team, player, amount, reason, default);
}
