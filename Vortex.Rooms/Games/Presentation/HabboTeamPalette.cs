using System.Collections.Generic;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.Presentation;

/// <summary>
/// The one place a domain team becomes a Habbo colour, and the reason nothing else has to.
/// <para>
/// The four colours are real, but they are real in exactly four places: the coloured furni families
/// (<c>bb_gate_r</c>, <c>fball_goal_b</c>, <c>es_score_g</c>), the team aura effect ids
/// (<c>base + colour</c>), the wired boxes whose radio ids are 1-4, and the scoreboards. All four are
/// presentation or protocol. A game's teams are its own, and this maps between the two.
/// </para>
/// <para>
/// A team is matched to a colour by its <see cref="GameTeam.Key"/>. A game whose teams are the Habbo
/// colours (every game shipped today, because their arenas are built from the coloured families) gets
/// a <see cref="IsComplete"/> palette and behaves exactly as before. A game with five teams, or with
/// teams called "hunters" and "hiders", maps what it can and leaves the rest
/// <see cref="GameTeamColor.None"/> — the coloured furniture genuinely cannot show them, and that
/// limit now sits here instead of in the shape of the team model.
/// </para>
/// </summary>
public sealed class HabboTeamPalette
{
    /// <summary>The colour names, in wire order, that a team key must match to earn a colour.</summary>
    private static readonly (string Key, GameTeamColor Colour)[] Named =
    [
        ("red", GameTeamColor.Red),
        ("green", GameTeamColor.Green),
        ("blue", GameTeamColor.Blue),
        ("yellow", GameTeamColor.Yellow),
    ];

    /// <summary>The palette for <see cref="TeamSet.HabboColours"/> — the one every shipped game uses.</summary>
    public static readonly HabboTeamPalette Standard = For(TeamSet.HabboColours);

    /// <summary>The four colours, in wire order. The ONLY sanctioned enumeration of them, and it
    /// lives here because the four are a Habbo fact about furniture and effects — a generic loop over
    /// "every team" walks a <see cref="TeamSet"/> instead.</summary>
    public static readonly IReadOnlyList<GameTeamColor> Colours =
    [
        GameTeamColor.Red,
        GameTeamColor.Green,
        GameTeamColor.Blue,
        GameTeamColor.Yellow,
    ];

    /// <summary>Whether that value names one of the four, rather than <c>None</c> or a stray cast.</summary>
    public static bool IsColour(GameTeamColor colour) =>
        colour is > GameTeamColor.None and <= GameTeamColor.Yellow;

    private readonly Dictionary<TeamId, GameTeamColor> _colourByTeam;
    private readonly Dictionary<GameTeamColor, TeamId> _teamByColour;

    private HabboTeamPalette(
        Dictionary<TeamId, GameTeamColor> colourByTeam,
        Dictionary<GameTeamColor, TeamId> teamByColour,
        bool isComplete
    )
    {
        _colourByTeam = colourByTeam;
        _teamByColour = teamByColour;
        IsComplete = isComplete;
    }

    /// <summary>
    /// Whether every team in the set has a Habbo colour — i.e. whether this game's teams can be
    /// addressed by the room's coloured furniture and wired boxes at all. The runtime uses it to
    /// decide whether an arena shares the room's Habbo-facing ledger or keeps its own.
    /// </summary>
    public bool IsComplete { get; }

    public static HabboTeamPalette For(TeamSet teams)
    {
        Dictionary<TeamId, GameTeamColor> colourByTeam = [];
        Dictionary<GameTeamColor, TeamId> teamByColour = [];
        bool complete = teams.Count > 0;

        foreach (GameTeam team in teams.Teams)
        {
            GameTeamColor colour = GameTeamColor.None;

            foreach ((string key, GameTeamColor named) in Named)
            {
                if (string.Equals(team.Key, key, System.StringComparison.OrdinalIgnoreCase))
                {
                    colour = named;

                    break;
                }
            }

            // A colour claimed twice is not a mapping: the second team keeps None rather than
            // silently stealing the first one's boards.
            if (colour == GameTeamColor.None || !teamByColour.TryAdd(colour, team.Id))
            {
                complete = false;

                continue;
            }

            colourByTeam[team.Id] = colour;
        }

        return new HabboTeamPalette(colourByTeam, teamByColour, complete);
    }

    /// <summary>The colour that presents this team, or <see cref="GameTeamColor.None"/> when no
    /// Habbo furni can.</summary>
    public GameTeamColor ColourOf(TeamId team) =>
        _colourByTeam.TryGetValue(team, out GameTeamColor colour) ? colour : GameTeamColor.None;

    /// <summary>The team a coloured furni or wired box is addressing, or <see cref="TeamId.None"/>
    /// when this game has no team of that colour.</summary>
    public TeamId TeamOf(GameTeamColor colour) =>
        _teamByColour.TryGetValue(colour, out TeamId team) ? team : TeamId.None;
}
