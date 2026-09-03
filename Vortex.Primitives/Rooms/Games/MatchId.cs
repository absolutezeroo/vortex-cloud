using System.Globalization;

namespace Vortex.Primitives.Rooms.Games;

/// <summary>
/// Identifies one match of one game in one room. The sequence is a per-room counter handed out by
/// the game runtime, so two successive Battle Banzai rounds in the same room are distinguishable —
/// which is exactly what a queued teleport, a snowball in flight or a rolling ball needs in order to
/// answer "am I still part of the match that created me?".
/// <para>
/// Without it the only defence against a callback from the previous round mutating the new one is
/// remembering to clear every queue at kick-off, and a missed queue fails silently. Every deferred
/// piece of work in a game module carries its <see cref="MatchId"/> and is dropped when it no longer
/// matches the live one.
/// </para>
/// </summary>
public readonly record struct MatchId(RoomId Room, GameId Game, int Sequence)
{
    public static readonly MatchId None = default;

    public bool IsNone => Sequence == 0;

    public override string ToString() =>
        IsNone
            ? "none"
            : string.Create(CultureInfo.InvariantCulture, $"{Room.Value}/{Game.Value}#{Sequence}");
}
