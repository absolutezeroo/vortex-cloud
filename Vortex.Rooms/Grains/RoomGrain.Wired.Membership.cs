using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Grains;

/// <summary>
/// The per-player facts a wired condition needs but cannot await for: guild rosters and worn badges.
/// Conditions evaluate synchronously inside the room's turn, so each box warms these caches from its
/// asynchronous prepare step and the evaluation itself is a set lookup.
/// </summary>
public sealed partial class RoomGrain
{
    /// <summary>How long a warmed roster or badge set is trusted. Long enough that a stack firing on
    /// a periodic trigger does not query per tick, short enough that joining a guild or moving a
    /// badge takes effect while the player is still in the room.</summary>
    private const long WiredMembershipTtlMs = 30_000;

    private readonly WiredTtlCache<int, HashSet<PlayerId>> _wiredGuildRosters = new(
        WiredMembershipTtlMs
    );

    private readonly WiredTtlCache<PlayerId, HashSet<string>> _wiredWornBadges = new(
        WiredMembershipTtlMs
    );

    internal async Task EnsureGuildRosterAsync(int groupId, CancellationToken ct)
    {
        if (groupId <= 0 || _wiredGuildRosters.IsFresh(groupId, NowMs()))
        {
            return;
        }

        // The room's own guild is already resident: RoomGrain.HydrateGroupMembershipAsync keeps its
        // roster in live state for the rights checks, so the overwhelmingly common case ("current
        // group" in a guild base) costs no query at all.
        if (_state.RoomSnapshot.GroupId == groupId)
        {
            _wiredGuildRosters.Set(groupId, [.. _state.GroupMemberRanks.Keys], NowMs());

            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<int> memberIds = await dbCtx
                .GroupMembers.AsNoTracking()
                .Where(m => m.GroupEntityId == groupId && m.DeletedAt == null)
                .Select(m => m.PlayerEntityId)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            _wiredGuildRosters.Set(groupId, [.. memberIds.Select(id => new PlayerId(id))], NowMs());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to load the wired guild roster for group {GroupId} in room {RoomId}.",
                groupId,
                RoomId
            );
        }
    }

    internal bool IsGuildMember(int groupId, PlayerId player) =>
        groupId > 0
        && _wiredGuildRosters.TryGet(groupId, NowMs(), out HashSet<PlayerId>? roster)
        && roster!.Contains(player);

    internal async Task EnsureWornBadgesAsync(PlayerId player, CancellationToken ct)
    {
        if (player <= 0 || _wiredWornBadges.IsFresh(player, NowMs()))
        {
            return;
        }

        try
        {
            ImmutableArray<PlayerBadgeSnapshot> badges = await _grainFactory
                .GetPlayerBadgeGrain(player)
                .GetBadgesAsync(ct)
                .ConfigureAwait(true);

            // Slot 0 is "owned but not displayed". The client's box says "wearing", and that is the
            // distinction a player can actually see on the avatar, so an owned-only badge must not
            // pass the condition.
            _wiredWornBadges.Set(
                player,
                [.. badges.Where(b => b.SlotId > 0).Select(b => b.BadgeCode)],
                NowMs()
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to load the wired worn badges for player {PlayerId} in room {RoomId}.",
                player,
                RoomId
            );
        }
    }

    internal bool IsWearingBadge(PlayerId player, string badgeCode) =>
        !string.IsNullOrWhiteSpace(badgeCode)
        && _wiredWornBadges.TryGet(player, NowMs(), out HashSet<string>? worn)
        && worn!.Contains(badgeCode);
}
