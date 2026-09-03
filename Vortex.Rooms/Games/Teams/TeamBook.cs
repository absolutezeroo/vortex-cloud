using System;
using System.Collections.Generic;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Games.Teams;

/// <summary>
/// A team + score ledger over a <see cref="TeamSet"/>: which team each player is on, each team's
/// score, and the per-match caps the wired score actions consume. Pure — no IO, no room, no packets,
/// no colours — so every rule in it is unit-testable on its own, and the runtime turns the "did
/// something change" answers into client updates.
/// <para>
/// Storage is keyed by <see cref="TeamId"/> rather than indexed by a colour, which is what lets a
/// game have two teams, or seven, or teams with no Habbo colour at all. The book knows its own set,
/// so "every team" is a walk of that set and never a count to four.
/// </para>
/// <para>
/// Member counts are always DERIVED from the membership map, never a parallel counter that could
/// drift.
/// </para>
/// </summary>
public sealed class TeamBook(TeamSet teams)
{
    private readonly Dictionary<PlayerId, TeamId> _teamByPlayer = [];
    private readonly Dictionary<TeamId, int> _scoreByTeam = [];

    // Per-match caps: GIVE_SCORE is capped per (score box, player); GIVE_SCORE_TO_TEAM per box.
    private readonly Dictionary<(RoomObjectId Box, PlayerId Player), int> _giveScoreUses = [];
    private readonly Dictionary<RoomObjectId, int> _giveTeamScoreUses = [];

    /// <summary>The teams this book knows. Everything that iterates teams iterates this.</summary>
    public TeamSet Teams { get; } = teams;

    /// <summary>Whether that id is a team of this book's set. Replaces the old "is it between Red and
    /// Yellow" test, which was really asking the same question of a hardcoded set.</summary>
    public bool Knows(TeamId team) => !team.IsNone && Teams.Contains(team);

    public TeamId GetTeam(PlayerId playerId) =>
        _teamByPlayer.TryGetValue(playerId, out TeamId team) ? team : TeamId.None;

    public int GetTeamScore(TeamId team) =>
        Knows(team) && _scoreByTeam.TryGetValue(team, out int score) ? score : 0;

    public int GetTeamMemberCount(TeamId team)
    {
        int count = 0;

        foreach (TeamId value in _teamByPlayer.Values)
        {
            if (value == team)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>How many of the set's teams currently have at least one member — what a game checks
    /// before arming an "only one team left standing" win condition.</summary>
    public int GetOccupiedTeamCount()
    {
        int occupied = 0;

        foreach (TeamId team in Teams.Ids())
        {
            if (GetTeamMemberCount(team) > 0)
            {
                occupied++;
            }
        }

        return occupied;
    }

    public IReadOnlyList<PlayerId> GetPlayersInTeam(TeamId team)
    {
        List<PlayerId> players = [];

        if (!Knows(team))
        {
            return players;
        }

        foreach ((PlayerId playerId, TeamId value) in _teamByPlayer)
        {
            if (value == team)
            {
                players.Add(playerId);
            }
        }

        return players;
    }

    /// <summary>The least-populated team (ties resolve to the set's own order). Used by the "join
    /// balanced team" option of the wired join-team action.</summary>
    public TeamId GetSmallestTeam()
    {
        TeamId smallest = TeamId.None;
        int smallestCount = int.MaxValue;

        foreach (TeamId team in Teams.Ids())
        {
            int count = GetTeamMemberCount(team);

            if (count < smallestCount)
            {
                smallestCount = count;
                smallest = team;
            }
        }

        return smallest;
    }

    /// <summary>Puts the player on <paramref name="team"/>, leaving any previous team. Returns true
    /// when the membership actually changed, i.e. the caller should broadcast the new team aura.</summary>
    public bool JoinTeam(PlayerId playerId, TeamId team)
    {
        if (!Knows(team) || GetTeam(playerId) == team)
        {
            return false;
        }

        _teamByPlayer[playerId] = team;

        return true;
    }

    /// <summary>Removes the player from their team. Returns true when they were on one.</summary>
    public bool LeaveTeam(PlayerId playerId) => _teamByPlayer.Remove(playerId);

    /// <summary>Awards points to the player's own team. False when the player is on no team or the
    /// per-(box, player) cap is exhausted.</summary>
    public bool TryGiveScoreToPlayerTeam(RoomObjectId box, PlayerId playerId, int amount, int cap)
    {
        TeamId team = GetTeam(playerId);

        if (team.IsNone || !TryConsumeUse(_giveScoreUses, (box, playerId), cap))
        {
            return false;
        }

        AddScore(team, amount);

        return true;
    }

    /// <summary>Awards points to a fixed team. False when the per-box cap is exhausted.</summary>
    public bool TryGiveScoreToTeam(RoomObjectId box, TeamId team, int amount, int cap)
    {
        if (!Knows(team) || !TryConsumeUse(_giveTeamScoreUses, box, cap))
        {
            return false;
        }

        AddScore(team, amount);

        return true;
    }

    /// <summary>Clears membership and per-player caps when a player leaves the room, so team state
    /// never outlives a player's presence.</summary>
    public void OnPlayerLeft(PlayerId playerId)
    {
        _teamByPlayer.Remove(playerId);

        foreach (
            (RoomObjectId Box, PlayerId Player) key in new List<(RoomObjectId, PlayerId)>(
                _giveScoreUses.Keys
            )
        )
        {
            if (key.Player == playerId)
            {
                _giveScoreUses.Remove(key);
            }
        }
    }

    /// <summary>Wipes teams, scores and caps and returns the players who were on a team so the caller
    /// can clear their now-stale auras.</summary>
    public IReadOnlyList<PlayerId> Reset()
    {
        List<PlayerId> members = [.. _teamByPlayer.Keys];

        _teamByPlayer.Clear();
        ResetScores();

        return members;
    }

    /// <summary>Zeroes every team's score and the per-match score caps while KEEPING membership —
    /// what a fresh match needs. Teams are picked at the gates before kick-off, so wiping them here
    /// would empty the arena.</summary>
    public void ResetScores()
    {
        _scoreByTeam.Clear();
        _giveScoreUses.Clear();
        _giveTeamScoreUses.Clear();
    }

    /// <summary>Awards (or deducts) points with no cap accounting — the path game rules score
    /// through. Team scores floor at 0. Ignored for a team this book does not know.</summary>
    public void AddScore(TeamId team, int amount)
    {
        if (!Knows(team))
        {
            return;
        }

        long updated = (long)GetTeamScore(team) + amount;

        _scoreByTeam[team] = (int)Math.Clamp(updated, 0, int.MaxValue);
    }

    /// <summary>The team with the highest score, or <see cref="TeamId.None"/> when nobody has scored.
    /// Ties resolve to the set's own order — which for the Habbo colours is the order the wired rank
    /// condition uses.</summary>
    public TeamId GetLeadingTeam()
    {
        TeamId best = TeamId.None;
        int bestScore = 0;

        foreach (TeamId team in Teams.Ids())
        {
            int score = GetTeamScore(team);

            if (score > bestScore)
            {
                bestScore = score;
                best = team;
            }
        }

        return best;
    }

    private static bool TryConsumeUse<TKey>(Dictionary<TKey, int> uses, TKey key, int cap)
        where TKey : notnull
    {
        if (cap <= 0)
        {
            return true; // 0 == unlimited; do not track a count we will never check.
        }

        int used = uses.TryGetValue(key, out int value) ? value : 0;

        if (used >= cap)
        {
            return false;
        }

        uses[key] = used + 1;

        return true;
    }
}
