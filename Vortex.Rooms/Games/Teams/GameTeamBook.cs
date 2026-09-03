using System;
using System.Collections.Generic;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Games.Teams;

/// <summary>
/// The room's one team + score ledger: which team each player is on, each team's score, and the
/// per-match caps the wired score actions consume. Pure — no IO, no room, no packets — so every rule
/// in it is unit-testable on its own, and the runtime turns the "did something change" answers into
/// client updates.
/// <para>
/// There is exactly one of these per room, shared by every game, because every wired team leaf reads
/// it: <c>wf_cnd_actor_in_team</c>, <c>wf_cnd_team_has_score</c>, <c>wf_cnd_team_has_rank</c>,
/// <c>wf_slc_users_team</c> and <c>wf_trg_score_achieved</c>. A game that kept its own teams would be
/// invisible to all of them — which is the bug this shape exists to prevent.
/// </para>
/// <para>Member counts are always DERIVED from the membership map, never a parallel counter that
/// could drift.</para>
/// </summary>
public sealed class GameTeamBook
{
    private readonly Dictionary<PlayerId, GameTeamColor> _teamByPlayer = [];

    // Team score indexed by (int)GameTeamColor (1..4); index 0 (None) is unused.
    private readonly int[] _scoreByTeam = new int[5];

    // Per-match caps: GIVE_SCORE is capped per (score box, player); GIVE_SCORE_TO_TEAM per box.
    private readonly Dictionary<(RoomObjectId Box, PlayerId Player), int> _giveScoreUses = [];
    private readonly Dictionary<RoomObjectId, int> _giveTeamScoreUses = [];

    public static bool IsRealTeam(GameTeamColor team) =>
        team is > GameTeamColor.None and <= GameTeamColor.Yellow;

    public GameTeamColor GetTeam(PlayerId playerId) =>
        _teamByPlayer.TryGetValue(playerId, out GameTeamColor team) ? team : GameTeamColor.None;

    public int GetTeamScore(GameTeamColor team) => IsRealTeam(team) ? _scoreByTeam[(int)team] : 0;

    public int GetTeamMemberCount(GameTeamColor team)
    {
        int count = 0;

        foreach (GameTeamColor value in _teamByPlayer.Values)
        {
            if (value == team)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>How many of the layout's teams currently have at least one member — what a game
    /// checks before arming an "only one team left standing" win condition.</summary>
    public int GetOccupiedTeamCount(TeamLayout layout)
    {
        int occupied = 0;

        foreach (GameTeamColor colour in layout.Colours)
        {
            if (GetTeamMemberCount(colour) > 0)
            {
                occupied++;
            }
        }

        return occupied;
    }

    public IReadOnlyList<PlayerId> GetPlayersInTeam(GameTeamColor team)
    {
        List<PlayerId> players = [];

        if (!IsRealTeam(team))
        {
            return players;
        }

        foreach ((PlayerId playerId, GameTeamColor value) in _teamByPlayer)
        {
            if (value == team)
            {
                players.Add(playerId);
            }
        }

        return players;
    }

    /// <summary>The layout's least-populated team (ties resolve to the layout's own order). Used by
    /// the "join balanced team" option of the wired join-team action.</summary>
    public GameTeamColor GetSmallestTeam(TeamLayout layout)
    {
        GameTeamColor smallest = GameTeamColor.None;
        int smallestCount = int.MaxValue;

        foreach (GameTeamColor colour in layout.Colours)
        {
            int count = GetTeamMemberCount(colour);

            if (count < smallestCount)
            {
                smallestCount = count;
                smallest = colour;
            }
        }

        return smallest;
    }

    /// <summary>Puts the player on <paramref name="team"/>, leaving any previous team. Returns true
    /// when the membership actually changed, i.e. the caller should broadcast the new team aura.</summary>
    public bool JoinTeam(PlayerId playerId, GameTeamColor team)
    {
        if (!IsRealTeam(team) || GetTeam(playerId) == team)
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
        GameTeamColor team = GetTeam(playerId);

        if (team == GameTeamColor.None || !TryConsumeUse(_giveScoreUses, (box, playerId), cap))
        {
            return false;
        }

        AddScore(team, amount);

        return true;
    }

    /// <summary>Awards points to a fixed team. False when the per-box cap is exhausted.</summary>
    public bool TryGiveScoreToTeam(RoomObjectId box, GameTeamColor team, int amount, int cap)
    {
        if (!IsRealTeam(team) || !TryConsumeUse(_giveTeamScoreUses, box, cap))
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
        Array.Clear(_scoreByTeam);
        _giveScoreUses.Clear();
        _giveTeamScoreUses.Clear();
    }

    /// <summary>Awards (or deducts) points with no cap accounting — the path game rules score
    /// through. Team scores floor at 0. Ignored for <see cref="GameTeamColor.None"/>.</summary>
    public void AddScore(GameTeamColor team, int amount)
    {
        if (!IsRealTeam(team))
        {
            return;
        }

        long updated = (long)_scoreByTeam[(int)team] + amount;

        _scoreByTeam[(int)team] = (int)Math.Clamp(updated, 0, int.MaxValue);
    }

    /// <summary>The team with the highest score, or <see cref="GameTeamColor.None"/> when nobody has
    /// scored. Ties resolve to the lowest colour — the order the wired rank condition uses.</summary>
    public GameTeamColor GetLeadingTeam()
    {
        GameTeamColor best = GameTeamColor.None;
        int bestScore = 0;

        for (int team = (int)GameTeamColor.Red; team <= (int)GameTeamColor.Yellow; team++)
        {
            if (_scoreByTeam[team] > bestScore)
            {
                bestScore = _scoreByTeam[team];
                best = (GameTeamColor)team;
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
