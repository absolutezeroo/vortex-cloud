using System.Globalization;

namespace Vortex.Primitives.Rooms.Games;

/// <summary>
/// One playable installation of one game in one room: the game, plus which of that game's
/// installations this is.
/// <para>
/// A room is not limited to one game, and it is not limited to one arena of a game either — two
/// Battle Banzai boards at opposite ends of a hall are two independent playfields, and a match on one
/// has nothing to do with the other. The arena, not the game, is therefore what starts, what holds a
/// match, what owns a phase and what a <c>MatchId</c> is minted against.
/// </para>
/// <para>
/// <see cref="Instance"/> is assigned by the framework when it partitions the game's furniture into
/// installations, and is stable for as long as that partition holds. A game whose furniture forms one
/// installation per room — which is every Habbo game, because the wire has no way to address a second
/// one — is always instance 0.
/// </para>
/// </summary>
public readonly record struct ArenaId(GameId Game, int Instance)
{
    public static readonly ArenaId None = default;

    public bool IsNone => Game.IsNone;

    public override string ToString() =>
        IsNone ? "none" : string.Create(CultureInfo.InvariantCulture, $"{Game.Value}#{Instance}");
}
