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
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Players;
using Vortex.Players.Configuration;
using Vortex.Primitives.Events;
using Vortex.Primitives.Groups;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Groups.Grains;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Server.Grains;

namespace Vortex.Players.Grains;

internal sealed class GroupGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IEventPublisher events,
    ILogger<GroupGrain> logger
) : Grain, IGroupGrain
{
    // RoleType values mirror the client member enum.
    private const int RoleOwner = 0;
    private const int RoleAdmin = 1;
    private const int RoleMember = 2;
    private const int RoleRequested = 3;

    // Join failure reason code (client HabboGroupJoinFailedMessageEvent).
    private const int JoinFailedNotOpen = 2;

    private int GroupId => (int)this.GetPrimaryKeyLong();

    public async Task<GroupDetailsSnapshot?> GetDetailsAsync(PlayerId viewer, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.AsNoTracking()
            .Include(g => g.RoomEntity)
            .Include(g => g.OwnerPlayerEntity)
            .Include(g => g.ForumSettings)
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);

        if (group is null)
        {
            return null;
        }

        int viewerId = viewer.Value;

        GroupMemberEntity? membership = await dbCtx
            .GroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(
                m =>
                    m.GroupEntityId == GroupId
                    && m.PlayerEntityId == viewerId
                    && m.DeletedAt == null,
                ct
            );

        bool hasPendingRequest =
            membership is null
            && await dbCtx
                .GroupMembershipRequests.AsNoTracking()
                .AnyAsync(
                    r =>
                        r.GroupEntityId == GroupId
                        && r.PlayerEntityId == viewerId
                        && r.DeletedAt == null,
                    ct
                );

        int totalMembers = await dbCtx.GroupMembers.CountAsync(
            m => m.GroupEntityId == GroupId && m.DeletedAt == null,
            ct
        );

        int pendingCount = await dbCtx.GroupMembershipRequests.CountAsync(
            r => r.GroupEntityId == GroupId && r.DeletedAt == null,
            ct
        );

        bool favourite = await dbCtx
            .Players.AsNoTracking()
            .Where(p => p.Id == viewerId)
            .Select(p => p.FavouriteGroupId == GroupId)
            .FirstOrDefaultAsync(ct);

        bool isOwner = group.OwnerPlayerEntityId == viewerId;
        bool isAdmin = isOwner || membership?.Rank == GroupMemberRank.Admin;

        GroupMembershipStatus status =
            membership is not null ? GroupMembershipStatus.Member
            : hasPendingRequest ? GroupMembershipStatus.RequestPending
            : GroupMembershipStatus.NotMember;

        return new GroupDetailsSnapshot
        {
            GroupId = group.Id,
            IsGuild = true,
            Type = (int)group.Type,
            Name = group.Name,
            Description = group.Description ?? string.Empty,
            BadgeCode = group.Badge,
            RoomId = group.RoomEntityId,
            RoomName = group.RoomEntity.Name,
            Status = status,
            TotalMembers = totalMembers,
            Favourite = favourite,
            CreationDate = group.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
            IsOwner = isOwner,
            IsAdmin = isAdmin,
            OwnerName = group.OwnerPlayerEntity.Name,
            OpenDetails = false,
            MembersCanDecorate = !group.AdminOnlyDecoration,
            PendingMemberCount = pendingCount,
            HasForum = group.ForumSettings?.Enabled ?? false,
        };
    }

    public async Task<GroupMemberRank?> GetMemberRankAsync(PlayerId player, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int playerId = player.Value;

        return await dbCtx
            .GroupMembers.AsNoTracking()
            .Where(m =>
                m.GroupEntityId == GroupId && m.PlayerEntityId == playerId && m.DeletedAt == null
            )
            .Select(m => (GroupMemberRank?)m.Rank)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<GroupMembersPageSnapshot?> GetMembersAsync(
        PlayerId viewer,
        int pageIndex,
        string userNameFilter,
        int searchType,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);

        if (group is null)
        {
            return null;
        }

        int viewerId = viewer.Value;
        GroupMemberEntity? viewerMembership = await dbCtx
            .GroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(
                m =>
                    m.GroupEntityId == GroupId
                    && m.PlayerEntityId == viewerId
                    && m.DeletedAt == null,
                ct
            );
        bool allowedToManage =
            group.OwnerPlayerEntityId == viewerId
            || viewerMembership?.Rank == GroupMemberRank.Admin;

        int membersPerPage = await this
            .GrainFactory.GetServerConfigGrain()
            .GetIntAsync(GroupConfig.MembersPerPageKey, GroupConfig.MembersPerPageDefault)
            .ConfigureAwait(true);

        string filter = userNameFilter?.Trim() ?? string.Empty;
        int skip = Math.Max(pageIndex, 0) * membersPerPage;

        List<GroupMemberSnapshot> members;
        int totalEntries;

        // searchType 1 = pending join requests; otherwise current members.
        if (searchType == 1)
        {
            IQueryable<GroupMembershipRequestEntity> query = dbCtx
                .GroupMembershipRequests.AsNoTracking()
                .Include(r => r.PlayerEntity)
                .Where(r => r.GroupEntityId == GroupId && r.DeletedAt == null);

            if (filter.Length > 0)
            {
                query = query.Where(r => r.PlayerEntity.Name.Contains(filter));
            }

            totalEntries = await query.CountAsync(ct);

            members =
            [
                .. (
                    await query.OrderBy(r => r.Id).Skip(skip).Take(membersPerPage).ToListAsync(ct)
                ).Select(r => new GroupMemberSnapshot
                {
                    RoleType = RoleRequested,
                    UserId = r.PlayerEntityId,
                    UserName = r.PlayerEntity.Name,
                    Figure = r.PlayerEntity.Figure,
                    MemberSince = r.CreatedAt.ToString("dd-MM-yyyy"),
                }),
            ];
        }
        else
        {
            IQueryable<GroupMemberEntity> query = dbCtx
                .GroupMembers.AsNoTracking()
                .Include(m => m.PlayerEntity)
                .Where(m => m.GroupEntityId == GroupId && m.DeletedAt == null);

            if (filter.Length > 0)
            {
                query = query.Where(m => m.PlayerEntity.Name.Contains(filter));
            }

            totalEntries = await query.CountAsync(ct);

            members =
            [
                .. (
                    await query
                        // Admins first, then members; stable by id within a rank.
                        .OrderByDescending(m => m.Rank)
                        .ThenBy(m => m.Id)
                        .Skip(skip)
                        .Take(membersPerPage)
                        .ToListAsync(ct)
                ).Select(m => new GroupMemberSnapshot
                {
                    RoleType =
                        m.PlayerEntityId == group.OwnerPlayerEntityId ? RoleOwner
                        : m.Rank == GroupMemberRank.Admin ? RoleAdmin
                        : RoleMember,
                    UserId = m.PlayerEntityId,
                    UserName = m.PlayerEntity.Name,
                    Figure = m.PlayerEntity.Figure,
                    MemberSince = m.CreatedAt.ToString("dd-MM-yyyy"),
                }),
            ];
        }

        return new GroupMembersPageSnapshot
        {
            GroupId = group.Id,
            GroupName = group.Name,
            BaseRoomId = group.RoomEntityId,
            BadgeCode = group.Badge,
            TotalEntries = totalEntries,
            Members = members,
            AllowedToManage = allowedToManage,
            PageSize = membersPerPage,
            PageIndex = pageIndex,
            SearchType = searchType,
            UserNameFilter = filter,
        };
    }

    public async Task<int?> JoinAsync(PlayerId player, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

                await events
                    .PublishAsync(new GroupMembershipRequestedEvent(playerId, GroupId), ct)
                    .ConfigureAwait(true);

                await NotifyAdminsOfRequestAsync(
                        dbCtx,
                        group.OwnerPlayerEntityId,
                        new GroupMemberSnapshot
                        {
                            RoleType = RoleRequested,
                            UserId = playerId,
                            UserName = playerEntity.Name,
                            Figure = playerEntity.Figure,
                            MemberSince = DateTime.UtcNow.ToString("dd-MM-yyyy"),
                        },
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

        await events
            .PublishAsync(new GroupMemberJoinedEvent(playerId, GroupId), ct)
            .ConfigureAwait(true);

        await NotifyBaseRoomAsync([playerId], ct).ConfigureAwait(true);

        logger.LogInformation("Player {PlayerId} joined group {GroupId}", playerId, GroupId);
        return null;
    }

    public async Task<GroupEditInfoSnapshot?> GetEditInfoAsync(PlayerId actor, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);
        if (group is null)
        {
            return null;
        }

        int actorId = actor.Value;
        bool isOwner = group.OwnerPlayerEntityId == actorId;
        bool isAdmin =
            isOwner
            || await dbCtx.GroupMembers.AnyAsync(
                m =>
                    m.GroupEntityId == GroupId
                    && m.PlayerEntityId == actorId
                    && m.Rank == GroupMemberRank.Admin
                    && m.DeletedAt == null,
                ct
            );
        if (!isAdmin)
        {
            return null;
        }

        List<GroupRoomSnapshot> ownedRooms = await dbCtx
            .Rooms.AsNoTracking()
            .Where(r => r.PlayerEntityId == group.OwnerPlayerEntityId && r.DeletedAt == null)
            .OrderBy(r => r.Id)
            .Select(r => new GroupRoomSnapshot
            {
                RoomId = r.Id,
                RoomName = r.Name,
                HasControllers = r.GroupEntityId == null || r.Id == group.RoomEntityId,
            })
            .ToListAsync(ct);

        int membershipCount = await dbCtx.GroupMembers.CountAsync(
            m => m.GroupEntityId == GroupId && m.DeletedAt == null,
            ct
        );

        return new GroupEditInfoSnapshot
        {
            OwnedRooms = ownedRooms,
            IsOwner = isOwner,
            GroupId = group.Id,
            GroupName = group.Name,
            GroupDescription = group.Description ?? string.Empty,
            BaseRoomId = group.RoomEntityId,
            PrimaryColorId = ParseColorId(group.ColorOne),
            SecondaryColorId = ParseColorId(group.ColorTwo),
            GuildType = (int)group.Type,
            GuildRightsLevel = group.AdminOnlyDecoration ? 1 : 0,
            Locked = group.Type == GroupType.Private,
            Url = string.Empty,
            // Parse the stored badge code into exactly 5 layer entries (one per client layer).
            // The client iterates all 5 unconditionally — a short list causes a null-reference crash.
            BadgeParts = GuildBadgeLibrary.ParseBadgeCode(group.Badge),
            BadgeCode = group.Badge,
            MembershipCount = membershipCount,
        };
    }

    public async Task<bool> UpdateIdentityAsync(
        PlayerId actor,
        string name,
        string description,
        CancellationToken ct
    )
    {
        // Renaming must clear the same bar as creating: without this an admin could rename a guild
        // to an empty string, or to something past the column's 50-char limit (which would throw at
        // SaveChanges rather than being refused cleanly).
        int maxNameLength = await this
            .GrainFactory.GetServerConfigGrain()
            .GetIntAsync(GroupConfig.MaxNameLengthKey, GroupConfig.MaxNameLengthDefault)
            .ConfigureAwait(true);

        if (GroupNameRules.Validate(name, maxNameLength) is string reason)
        {
            logger.LogInformation(
                "Rejected rename of group {GroupId} by player {ActorId}: {Reason}",
                GroupId,
                actor.Value,
                reason
            );
            return false;
        }

        string trimmedName = name.Trim();

        return await MutateAsAdminAsync(
                actor,
                group =>
                {
                    group.Name = trimmedName;
                    group.Description = string.IsNullOrEmpty(description) ? null : description;
                },
                ct
            )
            .ConfigureAwait(true);
    }

    public Task<bool> UpdateColorsAsync(
        PlayerId actor,
        int primaryColorId,
        int secondaryColorId,
        CancellationToken ct
    )
    {
        return MutateAsAdminAsync(
            actor,
            group =>
            {
                group.ColorOne = primaryColorId.ToString();
                group.ColorTwo = secondaryColorId.ToString();
            },
            ct
        );
    }

    public Task<bool> UpdateBadgeAsync(
        PlayerId actor,
        IReadOnlyList<int> badgeParts,
        CancellationToken ct
    )
    {
        return MutateAsAdminAsync(
            actor,
            group => group.Badge = GroupDirectoryGrain.BuildBadgeCode(badgeParts),
            ct
        );
    }

    public async Task<bool> UpdateSettingsAsync(
        PlayerId actor,
        int guildType,
        int rightsLevel,
        CancellationToken ct
    )
    {
        // Settings (join policy + decoration rights) are owner-only.
        bool updated = await MutateAsync(
                actor,
                true,
                group =>
                {
                    if (guildType is >= 0 and <= 2)
                    {
                        group.Type = (GroupType)guildType;
                    }

                    group.AdminOnlyDecoration = rightsLevel != 0;
                },
                ct
            )
            .ConfigureAwait(true);

        if (updated)
        {
            // The decoration policy moves every plain member's build rights at once — an empty
            // affected list makes the room re-evaluate everyone standing in it.
            await NotifyBaseRoomAsync([], ct).ConfigureAwait(true);
        }

        return updated;
    }

    public async Task<bool> DeactivateAsync(PlayerId actor, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx.Groups.FirstOrDefaultAsync(
            g => g.Id == GroupId && g.DeletedAt == null,
            ct
        );
        if (group is null || group.OwnerPlayerEntityId != actor.Value)
        {
            return false;
        }

        DateTime now = DateTime.UtcNow;
        int baseRoomId = group.RoomEntityId;

        // Detach the room (clears the circular link), then soft-delete the group graph.
        await dbCtx
            .Rooms.Where(r => r.GroupEntityId == GroupId)
            .ExecuteUpdateAsync(up => up.SetProperty(r => r.GroupEntityId, (int?)null), ct)
            .ConfigureAwait(true);

        await dbCtx
            .GroupMembers.Where(m => m.GroupEntityId == GroupId && m.DeletedAt == null)
            .ExecuteUpdateAsync(up => up.SetProperty(m => m.DeletedAt, now), ct)
            .ConfigureAwait(true);

        await dbCtx
            .GroupMembershipRequests.Where(r => r.GroupEntityId == GroupId && r.DeletedAt == null)
            .ExecuteUpdateAsync(up => up.SetProperty(r => r.DeletedAt, now), ct)
            .ConfigureAwait(true);

        group.DeletedAt = now;
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await events
            .PublishAsync(new GroupDeactivatedEvent(actor.Value, GroupId), ct)
            .ConfigureAwait(true);

        // The room is no longer a guild base — drop the cached roster so ex-members immediately
        // lose the build rights the guild was granting them.
        await NotifyRoomAsync(baseRoomId, [], ct).ConfigureAwait(true);

        logger.LogInformation(
            "Group {GroupId} deactivated by player {ActorId}",
            GroupId,
            actor.Value
        );
        return true;
    }

    public async Task<GroupMemberSnapshot?> ApproveMembershipAsync(
        PlayerId actor,
        int targetPlayerId,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

        await events
            .PublishAsync(
                new GroupMembershipAcceptedEvent(actor.Value, GroupId, targetPlayerId),
                ct
            )
            .ConfigureAwait(true);

        await NotifyBaseRoomAsync([targetPlayerId], ct).ConfigureAwait(true);

        return new GroupMemberSnapshot
        {
            RoleType = RoleMember,
            UserId = targetPlayerId,
            UserName = request.PlayerEntity.Name,
            Figure = request.PlayerEntity.Figure,
            MemberSince = DateTime.UtcNow.ToString("dd-MM-yyyy"),
        };
    }

    public async Task<bool> RejectMembershipAsync(
        PlayerId actor,
        int targetPlayerId,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

        await events
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
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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
            added.Add(
                new GroupMemberSnapshot
                {
                    RoleType = RoleMember,
                    UserId = request.PlayerEntityId,
                    UserName = request.PlayerEntity.Name,
                    Figure = request.PlayerEntity.Figure,
                    MemberSince = now.ToString("dd-MM-yyyy"),
                }
            );
        }

        dbCtx.GroupMembershipRequests.RemoveRange(requests);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await Task.WhenAll(
                added.Select(member =>
                    events.PublishAsync(
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
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

        await events
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
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

        logger.LogInformation(
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
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

        await events
            .PublishAsync(
                new GroupMemberRankChangedEvent(actor.Value, GroupId, targetPlayerId, isAdmin),
                ct
            )
            .ConfigureAwait(true);

        await NotifyBaseRoomAsync([targetPlayerId], ct).ConfigureAwait(true);

        return new GroupMemberSnapshot
        {
            RoleType = isAdmin ? RoleAdmin : RoleMember,
            UserId = targetPlayerId,
            UserName = member.PlayerEntity.Name,
            Figure = member.PlayerEntity.Figure,
            MemberSince = member.CreatedAt.ToString("dd-MM-yyyy"),
        };
    }

    public async Task<int> GetMemberFurniCountAsync(int targetPlayerId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int? baseRoomId = await dbCtx
            .Groups.AsNoTracking()
            .Where(g => g.Id == GroupId && g.DeletedAt == null)
            .Select(g => (int?)g.RoomEntityId)
            .FirstOrDefaultAsync(ct);

        if (baseRoomId is null)
        {
            return 0;
        }

        return await dbCtx.Furnitures.CountAsync(
            f =>
                f.PlayerEntityId == targetPlayerId
                && f.RoomEntityId == baseRoomId
                && f.DeletedAt == null,
            ct
        );
    }

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
            logger.LogError(
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
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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
            ImmutableArray<RoomSummarySnapshot> activeRooms = await this
                .GrainFactory.GetRoomDirectoryGrain()
                .GetActiveRoomsAsync()
                .ConfigureAwait(true);

            if (!activeRooms.Any(r => r.RoomId.Value == roomId))
            {
                return;
            }

            await this
                .GrainFactory.GetRoomGrain(new RoomId(roomId))
                .RefreshGroupMembershipAsync(affectedPlayerIds, ct)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to refresh guild {GroupId} membership in its base room",
                GroupId
            );
        }
    }

    private Task<bool> MutateAsAdminAsync(
        PlayerId actor,
        Action<GroupEntity> mutate,
        CancellationToken ct
    )
    {
        return MutateAsync(actor, false, mutate, ct);
    }

    private async Task<bool> MutateAsync(
        PlayerId actor,
        bool ownerOnly,
        Action<GroupEntity> mutate,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx.Groups.FirstOrDefaultAsync(
            g => g.Id == GroupId && g.DeletedAt == null,
            ct
        );
        if (group is null)
        {
            return false;
        }

        int actorId = actor.Value;
        bool isOwner = group.OwnerPlayerEntityId == actorId;
        if (ownerOnly && !isOwner)
        {
            return false;
        }

        if (!isOwner)
        {
            bool isAdmin = await dbCtx.GroupMembers.AnyAsync(
                m =>
                    m.GroupEntityId == GroupId
                    && m.PlayerEntityId == actorId
                    && m.Rank == GroupMemberRank.Admin
                    && m.DeletedAt == null,
                ct
            );
            if (!isAdmin)
            {
                return false;
            }
        }

        mutate(group);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await events.PublishAsync(new GroupUpdatedEvent(actorId, GroupId), ct).ConfigureAwait(true);

        return true;
    }

    /// <summary>Loads the (tracked) group iff the actor is owner or admin; else null.</summary>
    private async Task<GroupEntity?> LoadIfAdminAsync(
        VortexDbContext dbCtx,
        PlayerId actor,
        CancellationToken ct
    )
    {
        GroupEntity? group = await dbCtx.Groups.FirstOrDefaultAsync(
            g => g.Id == GroupId && g.DeletedAt == null,
            ct
        );
        if (group is null)
        {
            return null;
        }

        int actorId = actor.Value;
        if (group.OwnerPlayerEntityId == actorId)
        {
            return group;
        }

        bool isAdmin = await dbCtx.GroupMembers.AnyAsync(
            m =>
                m.GroupEntityId == GroupId
                && m.PlayerEntityId == actorId
                && m.Rank == GroupMemberRank.Admin
                && m.DeletedAt == null,
            ct
        );

        return isAdmin ? group : null;
    }

    private static int ParseColorId(string value)
    {
        return int.TryParse(value, out int id) ? id : 0;
    }
}
