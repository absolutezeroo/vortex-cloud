using System.Collections.Generic;
using System.Collections.Immutable;

namespace Vortex.Rooms.Games.Teams;

/// <summary>
/// The teams one game plays with. Any number of them, named by the game, in the order a balancing
/// pick walks them.
/// <para>
/// This is where "how many teams does this game have" is answered, and it is why nothing else in the
/// framework counts to four. A two-team game declares two, an elimination free-for-all declares
/// seven, and a game whose teams have no Habbo colour declares whatever names it likes — the four
/// coloured furni families simply cannot present that game's teams, which is a presentation limit and
/// is where it now lives.
/// </para>
/// </summary>
public sealed record TeamSet
{
    /// <summary>
    /// The four Habbo colours, five a side. What every shipped game uses, because their arenas are
    /// built from the four coloured gate, goal and scoreboard families — so their teams genuinely are
    /// the Habbo colours, and the palette maps them one to one.
    /// </summary>
    public static readonly TeamSet HabboColours = Of("red", "green", "blue", "yellow")
        .WithCapacity(5);

    private TeamSet() { }

    /// <summary>The teams, ordinal 1..N in declaration order.</summary>
    public ImmutableArray<GameTeam> Teams { get; private init; } = [];

    /// <summary>How many distinct teams must hold at least one player for a match to be worth
    /// starting. 1 keeps solo practice possible; 2 is what an elimination game needs before it can
    /// treat "one team left" as a win.</summary>
    public int MinimumTeams { get; init; } = 1;

    public int Count => Teams.Length;

    /// <summary>Builds a set from team names, numbering them 1..N in the order given.</summary>
    public static TeamSet Of(params string[] keys)
    {
        ImmutableArray<GameTeam>.Builder builder = ImmutableArray.CreateBuilder<GameTeam>(
            keys.Length
        );

        for (int index = 0; index < keys.Length; index++)
        {
            builder.Add(new GameTeam { Id = new TeamId(index + 1), Key = keys[index] });
        }

        return new TeamSet { Teams = builder.ToImmutable() };
    }

    public TeamSet WithCapacity(int capacity)
    {
        ImmutableArray<GameTeam>.Builder builder = ImmutableArray.CreateBuilder<GameTeam>(
            Teams.Length
        );

        foreach (GameTeam team in Teams)
        {
            builder.Add(team with { Capacity = capacity });
        }

        return this with { Teams = builder.ToImmutable() };
    }

    public TeamSet WithMinimumTeams(int minimum) => this with { MinimumTeams = minimum };

    public bool Contains(TeamId id) => Find(id) is not null;

    /// <summary>
    /// Whether this set describes the SAME teams as another — same ids, same names, in the same
    /// order. Capacity is deliberately not compared: a game that takes the room's teams and caps them
    /// at three a side is still playing the room's teams, and must still share its ledger.
    /// </summary>
    public bool HasSameTeamsAs(TeamSet other)
    {
        if (other.Count != Count)
        {
            return false;
        }

        for (int index = 0; index < Teams.Length; index++)
        {
            if (
                Teams[index].Id != other.Teams[index].Id
                || !string.Equals(
                    Teams[index].Key,
                    other.Teams[index].Key,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return false;
            }
        }

        return true;
    }

    public GameTeam? Find(TeamId id)
    {
        foreach (GameTeam team in Teams)
        {
            if (team.Id == id)
            {
                return team;
            }
        }

        return null;
    }

    public GameTeam? FindByKey(string key)
    {
        foreach (GameTeam team in Teams)
        {
            if (string.Equals(team.Key, key, System.StringComparison.OrdinalIgnoreCase))
            {
                return team;
            }
        }

        return null;
    }

    public int CapacityOf(TeamId id) => Find(id)?.Capacity ?? 0;

    /// <summary>The ids, for the loops that used to walk Red..Yellow.</summary>
    public IEnumerable<TeamId> Ids()
    {
        foreach (GameTeam team in Teams)
        {
            yield return team.Id;
        }
    }
}
