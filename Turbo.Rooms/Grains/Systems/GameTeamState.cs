using System;
using System.Collections.Generic;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums.Games;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Rooms.Grains.Systems;

/// <summary>The pure, grain-independent team + score state for a room's wired game. This is the single
/// source of truth: which team each player is on, each team's score, and the per-game "times in game"
/// caps for the score actions. It has no IO — <see cref="RoomGameSystem"/> wraps it and turns the
/// returned "changed" signals into client effect broadcasts — so all of this logic is unit-testable in
/// isolation. Member counts are always DERIVED from the membership map (never a parallel counter that
/// could drift). Mutators return whether a visible change happened so the caller knows when to
/// broadcast/clear a team aura.</summary>
public sealed class GameTeamState
{
    private readonly Dictionary<PlayerId, GameTeamColor> _teamByPlayer = [];

    // Team score indexed by (int)GameTeamColor (1..4); index 0 (None) is unused.
    private readonly int[] _scoreByTeam = new int[5];

    // Per-game caps: GIVE_SCORE is capped per (score box, player); GIVE_SCORE_TO_TEAM per score box.
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

    /// <summary>The team with the fewest members (ties resolve to the lowest colour). Used by the
    /// "join balanced team" option of the join-team action.</summary>
    public GameTeamColor GetSmallestTeam()
    {
        GameTeamColor smallest = GameTeamColor.Red;
        int smallestCount = int.MaxValue;

        for (int color = (int)GameTeamColor.Red; color <= (int)GameTeamColor.Yellow; color++)
        {
            int count = GetTeamMemberCount((GameTeamColor)color);

            if (count < smallestCount)
            {
                smallestCount = count;
                smallest = (GameTeamColor)color;
            }
        }

        return smallest;
    }

    /// <summary>Puts the player on <paramref name="team"/> (leaving any previous team). Returns true
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

    /// <summary>Removes the player from their team. Returns true when they were on one, i.e. the caller
    /// should clear their aura.</summary>
    public bool LeaveTeam(PlayerId playerId) => _teamByPlayer.Remove(playerId);

    /// <summary>Awards points to the player's own team. Returns false when the player is on no team or
    /// the per-(box, player) cap is exhausted.</summary>
    public bool TryGiveScoreToPlayerTeam(RoomObjectId box, PlayerId playerId, int amount, int cap)
    {
        GameTeamColor team = GetTeam(playerId);

        if (team == GameTeamColor.None || !TryConsumeUse(_giveScoreUses, (box, playerId), cap))
        {
            return false;
        }

        AddTeamScore(team, amount);

        return true;
    }

    /// <summary>Awards points to a fixed team. Returns false when the per-box cap is exhausted.</summary>
    public bool TryGiveScoreToTeam(RoomObjectId box, GameTeamColor team, int amount, int cap)
    {
        if (!IsRealTeam(team) || !TryConsumeUse(_giveTeamScoreUses, box, cap))
        {
            return false;
        }

        AddTeamScore(team, amount);

        return true;
    }

    /// <summary>Clears membership and per-player caps when a player leaves the room, so team state never
    /// outlives a player's presence.</summary>
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

    /// <summary>Wipes teams, scores and caps (called when a game starts/restarts) and returns the
    /// players who were on a team so the caller can clear their now-stale auras.</summary>
    public IReadOnlyList<PlayerId> Reset()
    {
        List<PlayerId> members = [.. _teamByPlayer.Keys];

        _teamByPlayer.Clear();
        Array.Clear(_scoreByTeam);
        _giveScoreUses.Clear();
        _giveTeamScoreUses.Clear();

        return members;
    }

    private void AddTeamScore(GameTeamColor team, int amount)
    {
        // Team scores floor at 0 (a negative award cannot push a team below zero).
        long updated = (long)_scoreByTeam[(int)team] + amount;

        _scoreByTeam[(int)team] = (int)Math.Clamp(updated, 0, int.MaxValue);
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
