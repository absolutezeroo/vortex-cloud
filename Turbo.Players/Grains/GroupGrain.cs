using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Groups;
using Turbo.Database.Entities.Players;
using Turbo.Players.Configuration;
using Turbo.Primitives.Events;
using Turbo.Primitives.Groups.Enums;
using Turbo.Primitives.Groups.Grains;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Players.Grains;

internal sealed class GroupGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IEventPublisher events,
    ILogger<GroupGrain> logger,
    IOptions<GroupConfig> groupConfig
) : Grain, IGroupGrain
{
    private readonly GroupConfig _groupConfig = groupConfig.Value;

    // RoleType values mirror the client member enum.
    private const int RoleOwner = 0;
    private const int RoleAdmin = 1;
    private const int RoleMember = 2;
    private const int RoleRequested = 3;

    // Membership status the detail view reports for the viewer.
    private const int StatusNotMember = 0;
    private const int StatusMember = 1;
    private const int StatusRequested = 2;

    // Join failure reason code (client HabboGroupJoinFailedMessageEvent).
    private const int JoinFailedNotOpen = 2;

    private int GroupId => (int)this.GetPrimaryKeyLong();

    // ── Reads ───────────────────────────────────────────────────────────────────

    public async Task<GroupDetailsSnapshot?> GetDetailsAsync(PlayerId viewer, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

        int status =
            membership is not null ? StatusMember
            : hasPendingRequest ? StatusRequested
            : StatusNotMember;

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

    public async Task<GroupMembersPageSnapshot?> GetMembersAsync(
        PlayerId viewer,
        int pageIndex,
        string userNameFilter,
        int searchType,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

        string filter = userNameFilter?.Trim() ?? string.Empty;
        int skip = Math.Max(pageIndex, 0) * _groupConfig.MembersPerPage;

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
                    await query
                        .OrderBy(r => r.Id)
                        .Skip(skip)
                        .Take(_groupConfig.MembersPerPage)
                        .ToListAsync(ct)
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
                        .Take(_groupConfig.MembersPerPage)
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
            PageSize = _groupConfig.MembersPerPage,
            PageIndex = pageIndex,
            SearchType = searchType,
            UserNameFilter = filter,
        };
    }

    // ── Join ────────────────────────────────────────────────────────────────────

    public async Task<int?> JoinAsync(PlayerId player, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);

        if (group is null || group.Type == GroupType.Private)
        {
            return JoinFailedNotOpen;
        }

        int playerId = player.Value;

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
                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

                await events
                    .PublishAsync(new GroupMembershipRequestedEvent(playerId, GroupId), ct)
                    .ConfigureAwait(false);
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
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(new GroupMemberJoinedEvent(playerId, GroupId), ct)
            .ConfigureAwait(false);

        logger.LogInformation("Player {PlayerId} joined group {GroupId}", playerId, GroupId);
        return null;
    }

    // ── Management ────────────────────────────────────────────────────────────────

    public async Task<GroupEditInfoSnapshot?> GetEditInfoAsync(PlayerId actor, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

    public Task<bool> UpdateIdentityAsync(
        PlayerId actor,
        string name,
        string description,
        CancellationToken ct
    )
    {
        return MutateAsAdminAsync(
            actor,
            group =>
            {
                group.Name = name;
                group.Description = string.IsNullOrEmpty(description) ? null : description;
            },
            ct
        );
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

    public Task<bool> UpdateSettingsAsync(
        PlayerId actor,
        int guildType,
        int rightsLevel,
        CancellationToken ct
    )
    {
        // Settings (join policy + decoration rights) are owner-only.
        return MutateAsync(
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
        );
    }

    public async Task<bool> DeactivateAsync(PlayerId actor, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx.Groups.FirstOrDefaultAsync(
            g => g.Id == GroupId && g.DeletedAt == null,
            ct
        );
        if (group is null || group.OwnerPlayerEntityId != actor.Value)
        {
            return false;
        }

        DateTime now = DateTime.UtcNow;

        // Detach the room (clears the circular link), then soft-delete the group graph.
        await dbCtx
            .Rooms.Where(r => r.GroupEntityId == GroupId)
            .ExecuteUpdateAsync(up => up.SetProperty(r => r.GroupEntityId, (int?)null), ct)
            .ConfigureAwait(false);

        await dbCtx
            .GroupMembers.Where(m => m.GroupEntityId == GroupId && m.DeletedAt == null)
            .ExecuteUpdateAsync(up => up.SetProperty(m => m.DeletedAt, now), ct)
            .ConfigureAwait(false);

        await dbCtx
            .GroupMembershipRequests.Where(r => r.GroupEntityId == GroupId && r.DeletedAt == null)
            .ExecuteUpdateAsync(up => up.SetProperty(r => r.DeletedAt, now), ct)
            .ConfigureAwait(false);

        group.DeletedAt = now;
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(new GroupDeactivatedEvent(actor.Value, GroupId), ct)
            .ConfigureAwait(false);

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
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(
                new GroupMembershipAcceptedEvent(actor.Value, GroupId, targetPlayerId),
                ct
            )
            .ConfigureAwait(false);

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
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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
            .ConfigureAwait(false);

        if (deleted == 0)
        {
            return false;
        }

        await events
            .PublishAsync(
                new GroupMembershipRejectedEvent(actor.Value, GroupId, targetPlayerId),
                ct
            )
            .ConfigureAwait(false);

        return true;
    }

    public async Task<List<GroupMemberSnapshot>> ApproveAllMembershipsAsync(
        PlayerId actor,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await Task.WhenAll(
                added.Select(member =>
                    events.PublishAsync(
                        new GroupMembershipAcceptedEvent(actor.Value, GroupId, member.UserId),
                        ct
                    )
                )
            )
            .ConfigureAwait(false);

        return added;
    }

    public async Task<bool> KickMemberAsync(
        PlayerId actor,
        int targetPlayerId,
        bool block,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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
            .ConfigureAwait(false);

        // Also drop any pending request (covers kicking a requester / cleaning up).
        await dbCtx
            .GroupMembershipRequests.Where(r =>
                r.GroupEntityId == GroupId
                && r.PlayerEntityId == targetPlayerId
                && r.DeletedAt == null
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        if (removed == 0)
        {
            return false;
        }

        // NOTE: `block` (prevent rejoining) needs a per-group block list — not yet modelled; the
        // member is removed regardless. Tracked for a follow-up slice.
        await events
            .PublishAsync(new GroupMemberKickedEvent(actor.Value, GroupId, targetPlayerId), ct)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<GroupMemberSnapshot?> SetAdminRightsAsync(
        PlayerId actor,
        int targetPlayerId,
        bool isAdmin,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(
                new GroupMemberRankChangedEvent(actor.Value, GroupId, targetPlayerId, isAdmin),
                ct
            )
            .ConfigureAwait(false);

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
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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

    // ── Helpers ───────────────────────────────────────────────────────────────────

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
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

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
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(new GroupUpdatedEvent(actorId, GroupId), ct)
            .ConfigureAwait(false);

        return true;
    }

    /// <summary>Loads the (tracked) group iff the actor is owner or admin; else null.</summary>
    private async Task<GroupEntity?> LoadIfAdminAsync(
        TurboDbContext dbCtx,
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
