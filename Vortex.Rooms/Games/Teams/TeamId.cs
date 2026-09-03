using System.Globalization;

namespace Vortex.Rooms.Games.Teams;

/// <summary>
/// A team, as the framework understands one: an ordinal within its game's <see cref="TeamSet"/> and
/// nothing else. It is deliberately not a colour.
/// <para>
/// Habbo ships four coloured team auras, four coloured gate families and four wired radio ids, and
/// for a long time that made <c>GameTeamColor</c> the identity of a team everywhere — in the score
/// array, in the membership map, in every event. That is a presentation fact leaking into the domain:
/// it caps every game at four teams, forces a colourless team ("the hunters", "the seekers") to
/// borrow a colour it does not have, and makes "how many teams does this game have" unanswerable
/// without counting enum members.
/// </para>
/// <para>
/// So the core reasons about <see cref="TeamId"/>, a game declares its teams in a
/// <see cref="TeamSet"/>, and the Habbo colours survive exactly where they are real — on the
/// furniture, in the aura effect ids, on the wired boxes and on the scoreboards — behind
/// <c>HabboTeamPalette</c>.
/// </para>
/// </summary>
public readonly record struct TeamId(int Value)
{
    /// <summary>No team. The default, so an unset field is never accidentally team one.</summary>
    public static readonly TeamId None = default;

    public bool IsNone => Value <= 0;

    public override string ToString() =>
        IsNone ? "none" : Value.ToString(CultureInfo.InvariantCulture);
}
