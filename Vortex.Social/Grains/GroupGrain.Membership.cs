using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Events;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Social.Groups;

namespace Vortex.Social.Grains;

/// <summary>Membership: who belongs to the guild, at what rank, and who is kept out.</summary>
internal sealed partial class GroupGrain
{
    public async Task<int?> JoinAsync(PlayerId player, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);

        if (group is null || group.Type == GroupType.Private)
        {
            return JoinFailedNotOpen;
        }

        int playerId = player.Value;

        // A blocked player is refused before anything else — otherwise kicking with "block" would
        // only slow them down for as long as it takes to press join again.
        bool blocked = await dbCtx.GroupBlockedMembers.AnyAsync(
            b => b.GroupEntityId == GroupId && b.PlayerEntityId == playerId && b.DeletedAt == null,
            ct
        );

        if (blocked)
        {
            return JoinFailedNotOpen;
        }

        bool alreadyMember = await dbCtx.GroupMembers.AnyAsync(
            m => m.GroupEntityId == GroupId && m.PlayerEntityId == playerId && m.DeletedAt == null,
            ct
        );
        if (alreadyMember)
        {
            return null;
        }

        PlayerEntity? playerEntity = await dbCtx.Players.FindAsync([playerId], ct);
        GroupEntity? groupEntity = await dbCtx.Groups.FindAsync([GroupId], ct);
        if (playerEntity is null || groupEntity is null)
        {
            return JoinFailedNotOpen;
        }

        if (group.Type == GroupType.Exclusive)
        {
            bool existing = await dbCtx.GroupMembershipRequests.AnyAsync(
                r =>
                    r.GroupEntityId == GroupId
                    && r.PlayerEntityId == playerId
                    && r.DeletedAt == null,
                ct
            );
            if (!existing)
            {
                dbCtx.GroupMembershipRequests.Add(
                    new GroupMembershipRequestEntity
                    {
                        GroupEntityId = GroupId,
                        PlayerEntityId = playerId,
                        GroupEntity = groupEntity,
                        PlayerEntity = playerEntity,
                    }
                );
                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

                await _events
                    .PublishAsync(new GroupMembershipRequestedEvent(playerId, GroupId), ct)
                    .ConfigureAwait(true);

                await NotifyAdminsOfRequestAsync(
                        dbCtx,
                        group.OwnerPlayerEntityId,
                        GroupMemberMapper.NewRequest(playerEntity, DateTime.UtcNow),
                        ct
                    )
                    .ConfigureAwait(true);
            }

            return null;
        }

        dbCtx.GroupMembers.Add(
            new GroupMemberEntity
            {
                GroupEntityId = GroupId,
                PlayerEntityId = playerId,
                Rank = GroupMemberRank.Member,
                GroupEntity = groupEntity,
                PlayerEntity = playerEntity,
            }
        );
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await _events
            .PublishAsync(new GroupMemberJoinedEvent(playerId, GroupId), ct)
            .ConfigureAwait(true);

        await NotifyBaseRoomAsync([playerId], ct).ConfigureAwait(true);

        _logger.LogInformation("Player {PlayerId} joined group {GroupId}", playerId, GroupId);
        return null;
    }

    public async Task<GroupMemberSnapshot?> ApproveMembershipAsync(
        PlayerId actor,
        int targetPlayerId,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await LoadIfAdminAsync(dbCtx, actor, ct);
        if (group is null)
        {
            return null;
        }

        GroupMembershipRequestEntity? request = await dbCtx
            .GroupMembershipRequests.Include(r => r.PlayerEntity)
            .FirstOrDefaultAsync(
                r =>
                    r.GroupEntityId == GroupId
                    && r.PlayerEntityId == targetPlayerId
                    && r.DeletedAt == null,
                ct
            );
        if (request is null)
        {
            return null;
        }

        dbCtx.GroupMembershipRequests.Remove(request);
        dbCtx.GroupMembers.Add(
            new GroupMemberEntity
            {
                GroupEntityId = GroupId,
                PlayerEntityId = targetPlayerId,
                Rank = GroupMemberRank.Member,
                GroupEntity = group,
                PlayerEntity = request.PlayerEntity,
            }
        );
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await _events
            .PublishAsync(
                new GroupMembershipAcceptedEvent(actor.Value, GroupId, targetPlayerId),
                ct
            )
            .ConfigureAwait(true);

        await NotifyBaseRoomAsync([targetPlayerId], ct).ConfigureAwait(true);

        return GroupMemberMapper.NewMember(request.PlayerEntity, DateTime.UtcNow);
    }

    public async Task<bool> RejectMembershipAsync(
        PlayerId actor,
        int targetPlayerId,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        if (await LoadIfAdminAsync(dbCtx, actor, ct) is null)
        {
            return false;
        }

        int deleted = await dbCtx
            .GroupMembershipRequests.Where(r =>
                r.GroupEntityId == GroupId
                && r.PlayerEntityId == targetPlayerId
                && r.DeletedAt == null
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(true);

        if (deleted == 0)
        {
            return false;
        }

        await _events
            .PublishAsync(
                new GroupMembershipRejectedEvent(actor.Value, GroupId, targetPlayerId),
                ct
            )
            .ConfigureAwait(true);

        return true;
    }

    public async Task<List<GroupMemberSnapshot>> ApproveAllMembershipsAsync(
        PlayerId actor,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await LoadIfAdminAsync(dbCtx, actor, ct);
        if (group is null)
        {
            return [];
        }

        List<GroupMembershipRequestEntity> requests = await dbCtx
            .GroupMembershipRequests.Include(r => r.PlayerEntity)
            .Where(r => r.GroupEntityId == GroupId && r.DeletedAt == null)
            .ToListAsync(ct);

        if (requests.Count == 0)
        {
            return [];
        }

        DateTime now = DateTime.UtcNow;
        List<GroupMemberSnapshot> added = new(requests.Count);

        foreach (GroupMembershipRequestEntity request in requests)
        {
            dbCtx.GroupMembers.Add(
                new GroupMemberEntity
                {
                    GroupEntityId = GroupId,
                    PlayerEntityId = request.PlayerEntityId,
                    Rank = GroupMemberRank.Member,
                    GroupEntity = group,
                    PlayerEntity = request.PlayerEntity,
                }
            );
            added.Add(GroupMemberMapper.NewMember(request.PlayerEntity, now));
        }

        dbCtx.GroupMembershipRequests.RemoveRange(requests);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await Task.WhenAll(
                added.Select(member =>
                    _events.PublishAsync(
                        new GroupMembershipAcceptedEvent(actor.Value, GroupId, member.UserId),
                        ct
                    )
                )
            )
            .ConfigureAwait(true);

        await NotifyBaseRoomAsync([.. added.Select(m => m.UserId)], ct).ConfigureAwait(true);

        return added;
    }

    public async Task<bool> KickMemberAsync(
        PlayerId actor,
        int targetPlayerId,
        bool block,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await LoadIfAdminAsync(dbCtx, actor, ct);
        // The owner can never be removed.
        if (group is null || group.OwnerPlayerEntityId == targetPlayerId)
        {
            return false;
        }

        int removed = await dbCtx
            .GroupMembers.Where(m =>
                m.GroupEntityId == GroupId
                && m.PlayerEntityId == targetPlayerId
                && m.DeletedAt == null
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(true);

        // Also drop any pending request (covers kicking a requester / cleaning up).
        await dbCtx
            .GroupMembershipRequests.Where(r =>
                r.GroupEntityId == GroupId
                && r.PlayerEntityId == targetPlayerId
                && r.DeletedAt == null
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(true);

        if (removed == 0)
        {
            return false;
        }

        if (block)
        {
            bool alreadyBlocked = await dbCtx.GroupBlockedMembers.AnyAsync(
                b =>
                    b.GroupEntityId == GroupId
                    && b.PlayerEntityId == targetPlayerId
                    && b.DeletedAt == null,
                ct
            );

            if (!alreadyBlocked)
            {
                dbCtx.GroupBlockedMembers.Add(
                    new GroupBlockedMemberEntity
                    {
                        GroupEntityId = GroupId,
                        PlayerEntityId = targetPlayerId,
                        BlockedByPlayerEntityId = actor.Value,
                        GroupEntity = group,
                        PlayerEntity = null!,
                    }
                );

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }
        }

        await _events
            .PublishAsync(new GroupMemberKickedEvent(actor.Value, GroupId, targetPlayerId), ct)
            .ConfigureAwait(true);

        await NotifyBaseRoomAsync([targetPlayerId], ct).ConfigureAwait(true);

        return true;
    }

    public async Task<bool> UnblockMemberAsync(
        PlayerId actor,
        int targetPlayerId,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        if (await LoadIfAdminAsync(dbCtx, actor, ct) is null)
        {
            return false;
        }

        int removed = await dbCtx
            .GroupBlockedMembers.Where(b =>
                b.GroupEntityId == GroupId
                && b.PlayerEntityId == targetPlayerId
                && b.DeletedAt == null
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(true);

        if (removed == 0)
        {
            return false;
        }

        _logger.LogInformation(
            "Player {TargetId} unblocked from group {GroupId} by {ActorId}",
            targetPlayerId,
            GroupId,
            actor.Value
        );

        return true;
    }

    public async Task<GroupMemberSnapshot?> SetAdminRightsAsync(
        PlayerId actor,
        int targetPlayerId,
        bool isAdmin,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx.Groups.FirstOrDefaultAsync(
            g => g.Id == GroupId && g.DeletedAt == null,
            ct
        );
        // Only the owner may change admin rights, and never their own.
        if (
            group is null
            || group.OwnerPlayerEntityId != actor.Value
            || targetPlayerId == actor.Value
        )
        {
            return null;
        }

        GroupMemberEntity? member = await dbCtx
            .GroupMembers.Include(m => m.PlayerEntity)
            .FirstOrDefaultAsync(
                m =>
                    m.GroupEntityId == GroupId
                    && m.PlayerEntityId == targetPlayerId
                    && m.DeletedAt == null,
                ct
            );
        if (member is null)
        {
            return null;
        }

        member.Rank = isAdmin ? GroupMemberRank.Admin : GroupMemberRank.Member;
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await _events
            .PublishAsync(
                new GroupMemberRankChangedEvent(actor.Value, GroupId, targetPlayerId, isAdmin),
                ct
            )
            .ConfigureAwait(true);

        await NotifyBaseRoomAsync([targetPlayerId], ct).ConfigureAwait(true);

        return GroupMemberMapper.FromMember(member, group.OwnerPlayerEntityId);
    }
}
