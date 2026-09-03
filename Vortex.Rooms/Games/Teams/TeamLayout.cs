using System.Collections.Immutable;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Rooms.Games.Teams;

/// <summary>
/// Which teams a game uses and how big they may get. The four colours themselves are a Habbo
/// protocol semantic, not a Vortex choice — the client ships exactly four team aura effects per set,
/// the wired team conditions are written over four radio ids, and the coloured furni families come
/// in four colours — so the room's team book is fixed at four slots. What a game gets to decide is
/// which of those slots it plays with, how many players fit in one, and whether an unbalanced
/// pick-up is allowed.
/// </summary>
public sealed record TeamLayout
{
    /// <summary>All four colours, five a side — what both gate-based games use.</summary>
    public static readonly TeamLayout FourColours = new();

    /// <summary>The colours this game plays with, in the order a balancing pick walks them.</summary>
    public ImmutableArray<GameTeamColor> Colours { get; init; } =
        [GameTeamColor.Red, GameTeamColor.Green, GameTeamColor.Blue, GameTeamColor.Yellow];

    /// <summary>Members allowed per team. 0 means unlimited.</summary>
    public int Capacity { get; init; } = 5;

    /// <summary>How many distinct teams must have at least one player for a match to be worth
    /// starting. 1 keeps solo practice possible; 2 is what an elimination game needs before it can
    /// treat "one team left" as a win.</summary>
    public int MinimumTeams { get; init; } = 1;

    public bool Uses(GameTeamColor colour) => Colours.Contains(colour);
}
