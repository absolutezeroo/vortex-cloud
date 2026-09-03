using System.Collections.Generic;
using Vortex.Primitives.Players;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.Freeze;

/// <summary>
/// Who is playing Freeze, and their per-player state — lives, ammo, boosts, the frozen and shield
/// timers. Pure rules with no IO, so the whole roster is unit-testable without a room; the module
/// turns what it returns into effects, teleports and score.
/// <para>
/// Membership is mirrored into the arena's <see cref="TeamBook"/> rather than kept only
/// here, because every wired team leaf reads that one: a Freeze player has to be a team member as
/// far as <c>wf_cnd_actor_in_team</c>, <c>wf_slc_users_team</c> and the team score and rank
/// conditions are concerned. What lives here is the state the shared ledger has no concept of.
/// </para>
/// </summary>
public sealed class FreezeRoster(TeamBook teams)
{
    private readonly Dictionary<PlayerId, FreezePlayerState> _players = [];
    private readonly TeamBook _teams = teams;

    public IReadOnlyDictionary<PlayerId, FreezePlayerState> Players => _players;

    public FreezePlayerState? Get(PlayerId playerId) =>
        _players.TryGetValue(playerId, out FreezePlayerState? player) ? player : null;

    /// <summary>Living members of a team — what a gate's counter shows and what the last-team-standing
    /// check counts.</summary>
    public int LivingCount(TeamId team)
    {
        int count = 0;

        foreach (FreezePlayerState player in _players.Values)
        {
            if (player.Team == team && !player.Dead)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>How many distinct teams still have at least one living player. A set rather than a
    /// bit mask, because a game's teams are however many it declared and not however many fit in an
    /// int.</summary>
    public int LivingTeamCount()
    {
        HashSet<TeamId> alive = [];

        foreach (FreezePlayerState player in _players.Values)
        {
            if (!player.Dead && _teams.Knows(player.Team))
            {
                alive.Add(player.Team);
            }
        }

        return alive.Count;
    }

    /// <summary>
    /// Toggles gate membership. The capacity check counts LIVING members of the target team, and it
    /// runs before the player leaves their current team, so a rejected switch never strips the
    /// membership they had.
    /// </summary>
    public TeamGateResult ToggleGate(
        TeamSet layout,
        PlayerId playerId,
        TeamId team,
        bool acceptingPlayers,
        FreezeSettings settings
    )
    {
        if (!acceptingPlayers || !layout.Contains(team) || !_teams.Knows(team))
        {
            return TeamGateResult.None;
        }

        if (
            _players.TryGetValue(playerId, out FreezePlayerState? existing)
            && existing.Team == team
        )
        {
            Remove(playerId);

            return TeamGateResult.Left;
        }

        int capacity = layout.CapacityOf(team);

        if (capacity > 0 && LivingCount(team) >= capacity)
        {
            return TeamGateResult.None;
        }

        _players.Remove(playerId); // leave the old team if switching
        _players[playerId] = new FreezePlayerState(playerId, team, settings);
        _teams.JoinTeam(playerId, team);

        return TeamGateResult.Joined;
    }

    /// <summary>Takes the player out of the game — they left the room, forfeited or were eliminated.
    /// They leave the shared team with it, so the wired team leaves stop counting someone who is no
    /// longer playing.</summary>
    public FreezePlayerState? Remove(PlayerId playerId)
    {
        if (!_players.Remove(playerId, out FreezePlayerState? player))
        {
            return null;
        }

        _teams.LeaveTeam(playerId);

        return player;
    }

    /// <summary>Everyone back to the starting loadout, adopting the match's freshly-resolved balance
    /// so a config edit reaches players who picked a gate before kick-off.</summary>
    public void ResetLoadouts(FreezeSettings settings)
    {
        foreach (FreezePlayerState player in _players.Values)
        {
            player.Reset(settings);
        }
    }

    /// <summary>Empties the roster. Membership in the shared ledger is left alone: the runtime owns
    /// that, and a match ending does not turn a player's team pick into nothing.</summary>
    public void Clear() => _players.Clear();
}
