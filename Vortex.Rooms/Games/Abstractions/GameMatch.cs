using System.Globalization;
using Vortex.Primitives.Rooms.Games;

namespace Vortex.Rooms.Games.Abstractions;

/// <summary>
/// The context of one match: its identity, when it started, and which round is being played. Every
/// piece of deferred work a game queues carries the <see cref="Id"/> it was created under and is
/// dropped when that no longer matches the live match — which is how a snowball thrown in the last
/// round cannot land in the next one, without any game having to remember to clear its queues.
/// </summary>
public sealed class GameMatch(MatchId id, long startedAtMs)
{
    public MatchId Id { get; } = id;

    public long StartedAtMs { get; } = startedAtMs;

    /// <summary>1-based round number within the match.</summary>
    public int Round { get; internal set; } = 1;

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Id} r{Round}");
}
