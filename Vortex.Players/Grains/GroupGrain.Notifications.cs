using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Users;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.Players.Grains;

/// <summary>
/// Outbound pushes. Every method here is best-effort: a guild change must not be rolled back
/// because a window somewhere failed to refresh, so failures are logged and swallowed.
/// </summary>
internal sealed partial class GroupGrain
{
    /// <summary>
    /// Pushes a pending join request to every admin (and the owner) so the guild-members window
    /// refreshes while it is open, instead of them discovering the request only on a manual reload.
    /// </summary>
    private async Task NotifyAdminsOfRequestAsync(
        VortexDbContext dbCtx,
        int ownerPlayerId,
        GroupMemberSnapshot requester,
        CancellationToken ct
    )
    {
        try
        {
            List<int> adminIds = await dbCtx
                .GroupMembers.AsNoTracking()
                .Where(m =>
                    m.GroupEntityId == GroupId
                    && m.DeletedAt == null
                    && (m.Rank == GroupMemberRank.Admin || m.PlayerEntityId == ownerPlayerId)
                )
                .Select(m => m.PlayerEntityId)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            if (adminIds.Count == 0)
            {
                return;
            }

            GroupMembershipRequestedMessageComposer composer = new()
            {
                GroupId = GroupId,
                Requester = requester,
            };

            // Independent grain calls — fan out in parallel rather than one round-trip per admin.
            await Task.WhenAll(
                    adminIds.Select(adminId =>
                        this.GrainFactory.GetPlayerPresenceGrain(adminId)
                            .SendComposerAsync(composer)
                    )
                )
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to notify admins of a membership request for group {GroupId}",
                GroupId
            );
        }
    }

    /// <summary>
    /// Pushes a membership/settings change into the guild's base room so guild-derived build rights
    /// take effect immediately. Deliberately skipped when the room grain is not already active —
    /// calling it would otherwise hydrate an empty room just to update a cache nobody is reading.
    /// </summary>
    /// <param name="affectedPlayerIds">
    /// Players whose controller level changed; empty means the guild's decoration policy moved and
    /// everyone currently in the room must be re-evaluated.
    /// </param>
    private async Task NotifyBaseRoomAsync(
        IReadOnlyList<int> affectedPlayerIds,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        int roomId = await dbCtx
            .Groups.AsNoTracking()
            .Where(g => g.Id == GroupId && g.DeletedAt == null)
            .Select(g => g.RoomEntityId)
            .FirstOrDefaultAsync(ct);

        if (roomId != 0)
        {
            await NotifyRoomAsync(roomId, affectedPlayerIds, ct).ConfigureAwait(true);
        }
    }

    /// <inheritdoc cref="NotifyBaseRoomAsync" />
    private async Task NotifyRoomAsync(
        int roomId,
        IReadOnlyList<int> affectedPlayerIds,
        CancellationToken ct
    )
    {
        try
        {
            ImmutableArray<RoomSummarySnapshot> activeRooms;

            using (
                _metrics.MeasureRoomDirectoryCall(nameof(IRoomDirectoryGrain.GetActiveRoomsAsync))
            )
            {
                activeRooms = await this
                    .GrainFactory.GetRoomDirectoryGrain()
                    .GetActiveRoomsAsync()
                    .ConfigureAwait(true);
            }

            if (!activeRooms.Any(r => r.RoomId.Value == roomId))
            {
                return;
            }

            await this
                .GrainFactory.GetRoomSecurity(new RoomId(roomId))
                .RefreshGroupMembershipAsync(affectedPlayerIds, ct)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to refresh guild {GroupId} membership in its base room",
                GroupId
            );
        }
    }
}
