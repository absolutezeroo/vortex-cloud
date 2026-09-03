using System.Globalization;

namespace Vortex.Primitives.Rooms.Games;

/// <summary>
/// Identifies one match on one arena in one room. The sequence is a per-arena counter handed out by
/// the game runtime, so two successive Battle Banzai rounds on the same board are distinguishable —
/// which is exactly what a queued teleport, a snowball in flight or a rolling ball needs in order to
/// answer "am I still part of the match that created me?".
/// <para>
/// It is keyed on the <see cref="ArenaId"/> rather than the game, so two boards of the same game
/// running at the same time are two matches and not one: deferred work from one board can never be
/// mistaken for the other's.
/// </para>
/// <para>
/// Without it the only defence against a callback from the previous round mutating the new one is
/// remembering to clear every queue at kick-off, and a missed queue fails silently. Every deferred
/// piece of work in a game module carries its <see cref="MatchId"/> and is dropped when it no longer
/// matches the live one.
/// </para>
/// </summary>
public readonly record struct MatchId(RoomId Room, ArenaId Arena, int Sequence)
{
    public static readonly MatchId None = default;

    public bool IsNone => Sequence == 0;

    /// <summary>The game being played, for the readers that only care which game it is.</summary>
    public GameId Game => Arena.Game;

    public override string ToString() =>
        IsNone
            ? "none"
            : string.Create(CultureInfo.InvariantCulture, $"{Room.Value}/{Arena}#{Sequence}");
}
